#pragma checksum "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\HtmlAgilityPackTool\Index.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "911c753855e86aea7314ff31645c682dd584bb63"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_HtmlAgilityPackTool_Index), @"mvc.1.0.view", @"/Views/HtmlAgilityPackTool/Index.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/HtmlAgilityPackTool/Index.cshtml", typeof(AspNetCore.Views_HtmlAgilityPackTool_Index))]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#line 1 "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\_ViewImports.cshtml"
using WebApp2.Controllers;

#line default
#line hidden
#line 2 "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\_ViewImports.cshtml"
using WebApp2.Models;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"911c753855e86aea7314ff31645c682dd584bb63", @"/Views/HtmlAgilityPackTool/Index.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"1347643869283a58b812fc2d68eae23dce8c8664", @"/Views/_ViewImports.cshtml")]
    public class Views_HtmlAgilityPackTool_Index : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<List<string>>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\HtmlAgilityPackTool\Index.cshtml"
  
    ViewData["Title"] = "Index";

#line default
#line hidden
            BeginContext(62, 31, true);
            WriteLiteral("\r\n<h2>Index</h2>\r\n\r\n<table>\r\n\r\n");
            EndContext();
#line 10 "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\HtmlAgilityPackTool\Index.cshtml"
     foreach (var item in Model)
    {

#line default
#line hidden
            BeginContext(134, 30, true);
            WriteLiteral("        <tr>\r\n            <td>");
            EndContext();
            BeginContext(165, 4, false);
#line 13 "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\HtmlAgilityPackTool\Index.cshtml"
           Write(item);

#line default
#line hidden
            EndContext();
            BeginContext(169, 22, true);
            WriteLiteral("</td>\r\n        </tr>\r\n");
            EndContext();
#line 15 "E:\GitSourceCode\NetCoreSample\WebApp\WebApp2\Views\HtmlAgilityPackTool\Index.cshtml"
    }

#line default
#line hidden
            BeginContext(198, 10, true);
            WriteLiteral("\r\n</table>");
            EndContext();
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<List<string>> Html { get; private set; }
    }
}
#pragma warning restore 1591