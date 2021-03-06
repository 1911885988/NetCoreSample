﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBook.Bee.Entity
{
    /// <summary>
    /// 任务类
    /// </summary>
    [Serializable]
    public class T8TaskEntity
    {
        public T8TaskEntity()
        {
            this.TaskGuid = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 任务的ID，是一个随机生成的GUID
        /// </summary>
        public string TaskGuid { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskTitle { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GenerateTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime CompleteTime { get; set; }

        /// <summary>
        /// 任务状态 （状态随着执行要更新对应状态）
        /// </summary>
        public T8TaskStatus T8TaskStatus { get; set; }

        // 任务源类型
        /// </summary>
        public TaskSourceType TaskSourceType { get; set; }

        /// <summary>
        /// 执行失败的次数 （任务失败要累加次数）
        /// </summary>
        public int ExecFailureTime { get; set; }

        /// <summary>
        /// 记录附加信息 （任务失败时附加上异步信息）
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 传8数据文件类
        /// </summary>
        public T8FileEntity T8FileEntity { get; set; }

        /// <summary>
        /// 生成任务队列Key
        /// </summary>
        public string GenerateTaskQueueKey
        {
            get
            {
                string fileName = "";
                if (this.T8FileEntity.GeneralFileInfo != null && !string.IsNullOrEmpty(this.T8FileEntity.GeneralFileInfo.FileName))
                {
                    fileName = this.T8FileEntity.GeneralFileInfo.FileName;
                    if (this.T8TaskStatus != T8TaskStatus.Error)
                    {
                        fileName = fileName.Replace(".", "_");
                    }
                    else
                    {
                        fileName = fileName.Replace(".", "_") + "_"+ DateTime.Now.ToString("yyyyMMdd");
                    }
                }
                return fileName;
            }
        }
    }
}
