﻿using AutoMapper;
using Openbook.Bee.Core.AutoFac;
using OpenBook.Bee.Entity;
using OpenBook.Bee.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Openbook.Bee.Core
{
    /// <summary>
    /// 任务加工
    /// </summary>
    public class TaskManager
    {
        /// <summary>
        /// T8配置项字典
        /// </summary>
        private ConcurrentDictionary<string, T8ConfigItemEntity> T8ConfigItemDic { get; set; }

        /// <summary>
        /// 完成任务队列
        /// Key 生成数据文件名 t8jyqssd_20181029_20181029_S_mdb
        /// </summary>
        private ConcurrentDictionary<string, T8TaskEntity> Completed_TaskQueue { get; set; }

        /// <summary>
        /// 正在处理任务队列 t8jyqssd_20181029_20181029_S_mdb
        /// </summary>
        private ConcurrentDictionary<string, T8TaskEntity> Processing_TaskQueue { get; set; }

        /// <summary>
        /// 错误任务队列 t8jyqssd_20181029_20181029_S_mdb_20181126 
        /// 记录天数时间戳表示当天不再执行 明天会继续执行
        /// </summary>
        private ConcurrentDictionary<string, T8TaskEntity> Error_TaskQueue { get; set; }

        /// <summary>
        /// 用户手工导出队列
        /// </summary>
        private ConcurrentDictionary<string, T8TaskEntity> UserManual_TaskQueue { get; set; }


        private CancellationTokenSource cts = new CancellationTokenSource();
        //CancellationToken token = cts.Token;
        //token.ThrowIfCancellationRequested();
        //cts.IsCancellationRequested

        public TaskManager():this(1)
        {
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">1普通轮询执行 2 Quartz调度作业</param>
        public TaskManager(int type)
        {
            Task.Factory.StartNew(() =>
            {
                T8ConfigItemDic = T8ConfigHelper.CloneT8ConfigItem();
                if (T8ConfigItemDic == null)
                {
                    T8ConfigItemDic = new ConcurrentDictionary<string, T8ConfigItemEntity>();
                }
            });

            if (type == 1)
            {
                //初始化任务队列
                InitTaskQueue();
            }

            else
            {
                T8ConfigItemDic = T8ConfigHelper.CloneT8ConfigItem();
                if (T8ConfigItemDic == null)
                {
                    T8ConfigItemDic = new ConcurrentDictionary<string, T8ConfigItemEntity>();
                }
            }
        }

        /// <summary>
        /// 任务处理开始
        /// </summary>
        public void Start()
        {
            LogUtil.WriteLog("========= TaskManager  Started==============");
            cts = new CancellationTokenSource();

            //检测T8配置项 为空重新获取
            Task.Factory.StartNew(() => CheckT8CofigItem(cts.Token));

            //迭代T8配置项
            Thread thread = new Thread(new ThreadStart(IteratonT8TaskItems));
            thread.Start();
        }

        /// <summary>
        /// 任务处理停止
        /// </summary>
        public void Stop()
        {
            cts.Cancel();
            LogUtil.WriteLog("========= TaskManager Stop===============");
        }

        /// <summary>
        /// 迭代T8配置项
        /// </summary>
        private void IteratonT8TaskItems()
        {
            int intervaltime = 1000 * 60 * 5;//迭代完毕间隔5分钟再重新迭代
            while (!cts.IsCancellationRequested)
            {
                foreach (KeyValuePair<string, T8ConfigItemEntity> item in T8ConfigItemDic)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }

                    T8ConfigItemEntity t8ConfigItem = item.Value;
                    TimingQueryTimeStragety timingStragety = new BuildInstanceObject().GetTimingQueryTimeStragety(t8ConfigItem.DateType);
                    DateTime TimingStartTime = timingStragety.GetStartTime(DateTime.Now);
                    DateTime TimingEndTime = timingStragety.GetStartTime(DateTime.Now);

                    if (DateTime.Now >= TimingStartTime && DateTime.Now <= TimingEndTime)
                    {
                        Task.Factory.StartNew(() => BuildServiceTask(t8ConfigItem, cts.Token));
                    }
                }

                for (int i = 0; i < intervaltime; i++)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// 构建T8任务
        /// </summary>
        private void BuildServiceTask(T8ConfigItemEntity t8ConfigItem, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                ACreateTask createTaskService = AutoFacContainer.ResolveNamed<ACreateTask>(typeof(ServiceCreateTask).Name);
                T8TaskEntity t8Task = createTaskService.CreateTask();
                if (t8Task != null)
                {
                    //获取文件名
                    AFileName aFileNameService = AutoFacContainer.ResolveNamed<AFileName>(typeof(GeneralFileName).Name);
                    t8Task.T8FileEntity.GeneralFileInfo = new T8FileInfoEntity
                    {
                        FileName = aFileNameService.FileName(t8Task.T8FileEntity),
                        FilePath = aFileNameService.FileFullName(t8Task.T8FileEntity)
                    };

                    T8TaskEntity taskEntityQueueItem;
                    if (Completed_TaskQueue.TryGetValue(t8Task.GenerateTaskQueueKey, out taskEntityQueueItem))
                    {
                        return;
                    }

                    if (Processing_TaskQueue.TryGetValue(t8Task.GenerateTaskQueueKey, out taskEntityQueueItem))
                    {
                        return;
                    }

                    if (Error_TaskQueue.TryGetValue(t8Task.GenerateTaskQueueKey, out taskEntityQueueItem))
                    {
                        return;
                    }

                    //添加到执行中队列   
                    if (!Common.AddTaskToQueue(Processing_TaskQueue, t8Task, TaskQueueType.Processing))
                    {
                        LogUtil.WriteLog($"任务队列[{t8Task.TaskTitle}]添加失败");

                        Common.SetTaskErrorStatus(t8Task, "添加Processing_TaskQueue失败");
                        Common.AddTaskToQueue(Error_TaskQueue, t8Task, TaskQueueType.Error);
                        return;
                    }

                    //构造数据文件产品并执行
                    DbFileProductDirector director = new DbFileProductDirector();
                    ADbFileProductBuilder productBuilder = new DbFileProductBuilder();
                    director.ConstructProduct(productBuilder);
                    DbFileProduct product = productBuilder.GetDbFileProduct();
                    product.Execute(t8Task, cts.Token);

                    //任务结束 1从Processing_TaskQueue队列移走 2任务完成，移入Completed_TaskQueue队列 3任务失败，移入
                    if (Common.RemoveTaskFromQueue(Processing_TaskQueue, t8Task, TaskQueueType.Processing))
                    {                      
                        if (t8Task.T8TaskStatus == T8TaskStatus.Complete)
                        {
                            Common.AddTaskToQueue(Completed_TaskQueue, t8Task, TaskQueueType.Completed);
                            LogUtil.WriteLog($"T8任务[{t8Task.TaskTitle}]执行完成，转移到Completed_TaskQueue队列");
                        }
                        else
                        {
                            Common.AddTaskToQueue(Error_TaskQueue, t8Task, TaskQueueType.Error);
                            LogUtil.WriteLog($"T8任务[{t8Task.TaskTitle}]执行失败，转移到Error_TaskQueue队列，等待下次重试");
                        }
                    }                  
                }
                else
                {
                    LogUtil.WriteLog("创建任务实体失败");
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteLog(ex);
            }
        }

        /// <summary>
        /// 检测T8ConfigItemDic是否为空
        /// </summary>
        private void CheckT8CofigItem(CancellationToken ct)
        {
            int intervaltime = 1000 * 30; //任务项为空，间隔30秒重新检测
            int intervaltime2 = 1000 * 1800;//任务项为空，休息30分钟重新检测

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (T8ConfigItemDic == null || T8ConfigItemDic.Keys.Count == 0)
                {
                    T8ConfigItemDic = T8ConfigHelper.CloneT8ConfigItem();
                    if (T8ConfigItemDic != null && T8ConfigItemDic.Keys.Count > 0)
                    {
                        for (int i = 0; i < intervaltime2; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            Thread.Sleep(1000);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < intervaltime; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        private void InitTaskQueue()
        {
            Task task1 = Task.Factory.StartNew(() =>
             {
                 Completed_TaskQueue = new ConcurrentDictionary<string, T8TaskEntity>();
                 string taskQueuePath = Common.GetTaskQueueFileFullpath(TaskQueueType.Completed);
                 List<T8TaskEntity> taskList = SerializableHelper<List<T8TaskEntity>>.BinaryDeserialize(taskQueuePath);
                 if (taskList != null && taskList.Count > 0)
                 {
                     foreach (T8TaskEntity t8Task in taskList)
                     {
                         Completed_TaskQueue.TryAdd(t8Task.GenerateTaskQueueKey, t8Task);
                     }
                 }
             });

            Task task2 = Task.Factory.StartNew(() =>
              {
                  Processing_TaskQueue = new ConcurrentDictionary<string, T8TaskEntity>();
                  string taskQueuePath = Common.GetTaskQueueFileFullpath(TaskQueueType.Processing);
                  List<T8TaskEntity> taskList = SerializableHelper<List<T8TaskEntity>>.BinaryDeserialize(taskQueuePath);
                  if (taskList != null && taskList.Count > 0)
                  {
                      foreach (T8TaskEntity t8Task in taskList)
                      {
                          Completed_TaskQueue.TryAdd(t8Task.GenerateTaskQueueKey, t8Task);
                      }
                  }
              });

            Task task3 = Task.Factory.StartNew(() =>
              {
                  Error_TaskQueue = new ConcurrentDictionary<string, T8TaskEntity>();
                  string taskQueuePath = Common.GetTaskQueueFileFullpath(TaskQueueType.Error);
                  List<T8TaskEntity> taskList = SerializableHelper<List<T8TaskEntity>>.BinaryDeserialize(taskQueuePath);
                  if (taskList != null && taskList.Count > 0)
                  {
                      foreach (T8TaskEntity t8Task in taskList)
                      {
                          Completed_TaskQueue.TryAdd(t8Task.GenerateTaskQueueKey, t8Task);
                      }
                  }
              });

            Task task4 = Task.Factory.StartNew(() =>
               {
                   UserManual_TaskQueue = new ConcurrentDictionary<string, T8TaskEntity>();
                   string taskQueuePath = Common.GetTaskQueueFileFullpath(TaskQueueType.UserManual);
                   List<T8TaskEntity> taskList = SerializableHelper<List<T8TaskEntity>>.BinaryDeserialize(taskQueuePath);
                   if (taskList != null && taskList.Count > 0)
                   {
                       foreach (T8TaskEntity t8Task in taskList)
                       {
                           Completed_TaskQueue.TryAdd(t8Task.GenerateTaskQueueKey, t8Task);
                       }
                   }
               });

            Task.WaitAll(new[] { task1, task2, task3, task4 });
        }
    }

    #region 创建T8任务实体
    /// <summary>
    /// 创建T8任务基类
    /// </summary>
    public abstract class ACreateTask
    {
        protected T8ConfigItemEntity _T8ConfigItemEntity;
        protected T8ConfigEntity _T8ConfigEntity;

        /// <summary>
        /// 设置初始化数据
        /// </summary>
        /// <param name="t8ConfigItem"></param>
        /// <param name="t8ConfigEntity"></param>
        public abstract void InitData(T8ConfigItemEntity t8ConfigItem, T8ConfigEntity t8ConfigEntity);

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <returns></returns>
        public abstract T8TaskEntity CreateTask();

        protected virtual SqlQueryTimeStragety GetISqlQueryTimeInstance(DateType dateType)
        {
            return new BuildInstanceObject().GetSqlQueryTimeStragety(dateType);
        }

        protected virtual T8TaskEntity GetNewTask(TaskSourceType taskSourceType)
        {
            T8FileEntity t8FileEntity = Mapper.Map<T8FileEntity>(this._T8ConfigItemEntity);
            t8FileEntity.DbFileType = this._T8ConfigEntity.DbFileType;
            t8FileEntity.FtpInfo = this._T8ConfigEntity.FtpInfo;
            t8FileEntity.DataBaseInfo = this._T8ConfigEntity.DataBaseInfo;
            t8FileEntity.TaskSourceType = taskSourceType;

            var sqlQueryService = this.GetISqlQueryTimeInstance(this._T8ConfigItemEntity.DateType);
            t8FileEntity.SqlStartTime = sqlQueryService.GetStartTime(DateTime.Now);
            t8FileEntity.SqlEndTime = sqlQueryService.GetEndTime(DateTime.Now);

            T8TaskEntity t8TaskEntity = new T8TaskEntity();
            t8TaskEntity.TaskTitle = Common.GetT8TaskTitle(this._T8ConfigItemEntity.DateType, this._T8ConfigItemEntity.DataType);
            t8TaskEntity.GenerateTime = DateTime.Now;
            t8TaskEntity.T8TaskStatus = T8TaskStatus.Created;
            t8TaskEntity.TaskSourceType = taskSourceType;
            t8TaskEntity.T8FileEntity = t8FileEntity;
            return t8TaskEntity;
        }
    }

    /// <summary>
    /// 服务创建T8任务
    /// </summary>
    public class ServiceCreateTask : ACreateTask
    {
        //public ServiceCreateTask(T8ConfigItemEntity t8ConfigItem, T8ConfigEntity t8ConfigEntity) : base(t8ConfigItem, t8ConfigEntity)
        //{
        //}

        public override void InitData(T8ConfigItemEntity t8ConfigItem, T8ConfigEntity t8ConfigEntity)
        {
            base._T8ConfigEntity = t8ConfigEntity;
            base._T8ConfigItemEntity = t8ConfigItem;
        }

        public override T8TaskEntity CreateTask()
        {
            try
            {
                return base.GetNewTask(TaskSourceType.Service);
            }
            catch (Exception ex)
            {
                LogUtil.WriteLog($"ServiceCreateTask.T8TaskEntity()异常,({ex.Message})");
                return null;
            }
        }

    }

    /// <summary>
    /// 用户手动操作创建T8任务
    /// </summary>
    public class UserCreateTask : ACreateTask
    {
        //public UserCreateTask(T8ConfigItemEntity t8ConfigItem, T8ConfigEntity t8ConfigEntity) : base(t8ConfigItem, t8ConfigEntity)
        //{
        //}

        public override void InitData(T8ConfigItemEntity t8ConfigItem, T8ConfigEntity t8ConfigEntity)
        {
            base._T8ConfigEntity = t8ConfigEntity;
            base._T8ConfigItemEntity = t8ConfigItem;
        }

        public override T8TaskEntity CreateTask()
        {
            try
            {
                return base.GetNewTask(TaskSourceType.User);
            }
            catch (Exception ex)
            {
                LogUtil.WriteLog($"ServiceCreateTask.T8TaskEntity()异常,({ex.Message})");
                return null;
            }
        }
    }
    #endregion

}
