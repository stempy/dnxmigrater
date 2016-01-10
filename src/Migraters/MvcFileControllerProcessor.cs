using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public class MvcFileControllerProcessor : FileProcessorBase, IFileCopyProcessor
    {
        public MvcFileControllerProcessor(ILogger logger) : base(logger)
        {
        }


        public bool CanProcessFile(string file)
        {
            return file.EndsWith("Controller.cs");
        }

        public IDictionary<string,string> ProcessFile(string file, string dest)
        {
            _log.Trace("\tprocessing controller {0} --> {1}", file, dest);
            var csTxt = UpdateMvcControllerFile(file);
            File.WriteAllText(dest, csTxt);
            _log.Trace("\tcontroller updated {0} --> {1}", file, dest);
            return null;
        }


        private string UpdateMvcControllerFile(string src)
        {
            // mvc controller file -- process in memory and save directly to path
            bool isApiFile = src.EndsWith("ApiController.cs");
            var csTxt = File.ReadAllText(src);
            // see: http://aspnetmvc.readthedocs.org/projects/mvc/en/latest/migration/migratingfromwebapi2.html

            //ApiController does not exist
            //System.Web.Http namespace does not exist
            //IHttpActionResult does not exist
            //NotFound does not exist
            //Ok does not exist
            //Fortunately, these are all very easy to correct:

            //Change ApiController to Controller(you may need to add using Microsoft.AspNet.Mvc)
            //Delete any using statement referring to System.Web.Http
            //Change any method returning IHttpActionResult to return a IActionResult
            //Change NotFound to HttpNotFound
            //Change Ok(product) to new ObjectResult(product)

            

            csTxt = csTxt.Replace(": ApiController", ": Controller");

            // using replacements
            if (csTxt.Contains("using System.Web.Mvc"))
                csTxt = csTxt.Replace("using System.Web.Mvc", "using Microsoft.AspNet.Mvc");
            if (csTxt.Contains("using System.Web.Http"))
                csTxt = csTxt.Replace("using System.Web.Http", "// dnxMigrater REMOVED - using System.Web.Http");

            // using additions
            var usings = new List<string>();
           

            if (!csTxt.Contains("using Microsoft.AspNet.Mvc;") || csTxt.Contains("//using Microsoft.AspNet.Mvc;"))
                usings.Add("Microsoft.AspNet.Mvc");
            if (csTxt.Contains("(FormCollection"))
                usings.Add("Microsoft.AspNet.Http.Internal");
            if (csTxt.Contains("[Authorize") || csTxt.Contains("[AllowAnonymous]"))
                usings.Add("Microsoft.AspNet.Authorization");
            if(csTxt.Contains("Request.RequestUri"))
                usings.Add("DnxMigrater.MVC6");
            if(csTxt.Contains("new SelectListItem"))
                usings.Add("Microsoft.AspNet.Mvc.Rendering");

            var newUsings = string.Join("\r\n", usings.Select(x=>"using "+x.TrimEnd(';')+";"));
            if (newUsings.Any())
                csTxt = newUsings +"\r\n"+ csTxt;

            csTxt = csTxt.Replace("IHttpActionResult", "IActionResult")
                .Replace(" NotFound", " HttpNotFound")
                .Replace("Ok(", "new ObjectResult(")
                .Replace("RoutePrefix", "Route")
                .Replace(" ActionResult "," IActionResult ")
                .Replace("Task<ActionResult>","Task<IActionResult>")
                .Replace("Request.RequestUri", "Request.RequestUri()")
                .Replace("BadRequest(","new BadRequestObjectResult(");


            return csTxt;
        }

    }
}