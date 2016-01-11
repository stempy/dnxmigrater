using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public class GenericFileProcessor : FileProcessorBase, IFileCopyProcessor
    {
        public GenericFileProcessor(ILogger logger) : base(logger)
        {
        }

        public bool CanProcessFile(string file)
        {
            return file.EndsWith(".cs") && !file.EndsWith("Config.cs");
        }


        private IDictionary<string, string> _dependenciesToAdd; 

        public IDictionary<string,string> ProcessFile(string file, string dest)
        {
            var csharp = File.ReadAllText(file);
            _dependenciesToAdd = new Dictionary<string, string>();
            var cleanedCsharp = CleanupCsharpFile(csharp);
            File.WriteAllText(dest, cleanedCsharp);
            _log.Trace("\t\tprocessed {0} --> {1}", file, dest);
            return _dependenciesToAdd;
        }

        private string CleanupCsharpFile(string csCode)
        {
            var mvc6final = "6.0.0-rc1-final";
            var mvc6finalServer = "1.0.0-rc1-final";
            var mvcDependency = "Microsoft.AspNet.Mvc";
            var usings = new List<string>();
            csCode = UpdateUsings(csCode);
            usings = GetUsings(csCode).ToList();
            if (csCode.Contains("MvcHtmlString") || csCode.Contains("this HtmlHelper"))
            {
                usings.Add("Microsoft.AspNet.Mvc.Rendering");
                usings.Add("Microsoft.AspNet.Mvc.ViewFeatures");

                _dependenciesToAdd.Add(mvcDependency,mvc6final);
                // html helper in file
                csCode = csCode.Replace("MvcHtmlString", "HtmlString");
            }
            if (usings.Any())
            {
                csCode = string.Join("\r\n", usings.Distinct().Select(x=>"using "+x+";")) + "\r\n" + csCode;
            }

            return csCode;
        }

        private ICollection<string> GetUsings(string csTxt)
        {
            var usings = new List<string>();

            if (csTxt.Contains("//using Microsoft.AspNet.Mvc;"))
                usings.Add("Microsoft.AspNet.Mvc");
            if (csTxt.Contains("(FormCollection"))
                usings.Add("Microsoft.AspNet.Http.Internal");
            if (csTxt.Contains("[Authorize") || csTxt.Contains("[AllowAnonymous]"))
                usings.Add("Microsoft.AspNet.Authorization");
            if (csTxt.Contains("Request.RequestUri"))
                usings.Add("DnxMigrater.MVC6");
            if (csTxt.Contains("new SelectListItem"))
                usings.Add("Microsoft.AspNet.Mvc.Rendering");
            if (csTxt.Contains(" HtmlHelper "))
                usings.Add("Microsoft.AspNet.Mvc.ViewFeatures");
            if (csTxt.Contains("(ApiDescription "))
                usings.Add("Microsoft.AspNet.Mvc.ApiExplorer");
            if (csTxt.Contains(" ModelStateDictionary") 
                || csTxt.Contains("(ModelMetadata") 
                || csTxt.Contains(" ModelMetadata "))
                    usings.Add("Microsoft.AspNet.Mvc.ModelBinding");

            return usings;
        } 

        private string UpdateUsings(string csTxt)
        {
            // mvc controller file -- process in memory and save directly to path
            // see: http://aspnetmvc.readthedocs.org/projects/mvc/en/latest/migration/migratingfromwebapi2.html
            // using replacements
            

            if (csTxt.Contains("using System.Web;"))
                csTxt = csTxt.Replace("using System.Web;", "// removed using System.Web;");

            if (csTxt.Contains("using System.Web.Mvc"))
                csTxt = csTxt.Replace("using System.Web.Mvc", "using Microsoft.AspNet.Mvc");
            if (csTxt.Contains("using System.Web.Http"))
                csTxt = csTxt.Replace("using System.Web.Http", "// dnxMigrater REMOVED - using System.Web.Http");
            // using additions

            if (csTxt.Contains("using Microsoft.AspNet.Mvc.Html;"))
                csTxt = csTxt.Replace("using Microsoft.AspNet.Mvc.Html;", "");

            if (csTxt.Contains("return htmlHelper."))
                csTxt = csTxt.Replace("return htmlHelper.", "return (HtmlString)htmlHelper.");


            csTxt = csTxt.Replace("HtmlHelper<", "IHtmlHelper<");


            return csTxt;
        }
    }
}