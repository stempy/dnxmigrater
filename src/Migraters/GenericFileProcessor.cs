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
            if (csCode.Contains("MvcHtmlString") || csCode.Contains("this HtmlHelper") || csCode.Contains("IHtmlHelper"))
            {
                usings.Add("Microsoft.AspNet.Mvc.Rendering");
                usings.Add("Microsoft.AspNet.Mvc.ViewFeatures");

                _dependenciesToAdd.Add(mvcDependency,mvc6final);
                // html helper in file
                csCode = csCode.Replace("MvcHtmlString", "HtmlString");
            }
            if (usings.Any())
            {
                var newUsings = usings.Where(x => !csCode.Contains("using " + x + ";"));
                csCode = string.Join("\r\n", newUsings.Distinct().Select(x=>"using "+x+";")) + "\r\n" + csCode;
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


        Dictionary<string,string> _replacementsDictionary; 

        private string UpdateUsings(string csTxt)
        {
            // mvc controller file -- process in memory and save directly to path
            // see: http://aspnetmvc.readthedocs.org/projects/mvc/en/latest/migration/migratingfromwebapi2.html
            // using replacements
            if (_replacementsDictionary == null)
            {
                _replacementsDictionary = new Dictionary<string, string>();
                _replacementsDictionary.Add("using System.Web;","// removed using System.Web");
                _replacementsDictionary.Add("using System.Web.Http;", "// dnxMigrater REMOVED - using System.Web.Http");
                _replacementsDictionary.Add("using System.Web.Html;", "// dnxMigrater REMOVED - using System.Web.Html");
                _replacementsDictionary.Add("using System.Web.Http.ModelBinding;", "using Microsoft.AspNet.Mvc.ModelBinding;");

                // mvc to aspnet.mvc
                _replacementsDictionary.Add("using System.Web.Mvc", "using Microsoft.AspNet.Mvc");
                _replacementsDictionary.Add("using System.Web.Routing", "using Microsoft.AspNet.Routing");

                _replacementsDictionary.Add("using Microsoft.AspNet.Mvc.Html;", "// dnxMigrater REMOVED - Microsoft.AspNet.Mvc.Html;");

                // html helpers
                _replacementsDictionary.Add("return htmlHelper.", "return (HtmlString)htmlHelper.");
                _replacementsDictionary.Add("HtmlHelper<", "IHtmlHelper<");
                _replacementsDictionary.Add("this HtmlHelper", "this IHtmlHelper");
                _replacementsDictionary.Add("(HtmlHelper ", "(IHtmlHelper ");
                _replacementsDictionary.Add(" HtmlHelper ", " IHtmlHelper ");


                _replacementsDictionary.Add("HttpUtility.HtmlEncode(", "System.Net.WebUtility.HtmlEncode(");
                _replacementsDictionary.Add("HttpUtility.HtmlDecode(", "System.Net.WebUtility.HtmlDecode(");
                _replacementsDictionary.Add("HttpUtility.UrlEncode(", "System.Net.WebUtility.UrlEncode(");
                _replacementsDictionary.Add("HttpUtility.UrlDecode(", "System.Net.WebUtility.UrlDecode(");


                _replacementsDictionary.Add("System.Web.Http.ModelBinding.ModelStateDictionary","ModelStateDictionary");
                _replacementsDictionary.Add("System.Web.Mvc.ModelStateDictionary", "ModelStateDictionary");
            }

            foreach (var item in _replacementsDictionary)
            {
                if (csTxt.Contains(item.Key))
                    csTxt = csTxt.Replace(item.Key, item.Value);
            }

            return csTxt;
        }
    }
}