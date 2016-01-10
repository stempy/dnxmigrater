using System.CodeDom;
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

        public void ProcessFile(string file, string dest)
        {
            _log.Trace("\tprocessing controller {0} --> {1}", file, dest);
            var csTxt = UpdateMvcControllerFile(file);
            File.WriteAllText(dest, csTxt);
            _log.Trace("\tcontroller updated {0} --> {1}", file, dest);
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

            if (csTxt.Contains("using System.Web.Mvc"))
            {
                csTxt = csTxt.Replace("using System.Web.Mvc", "using Microsoft.AspNet.Mvc");
            }

            if (!csTxt.Contains("Microsoft.AspNet.Mvc"))
            {
                csTxt = "using Microsoft.AspNet.Mvc;\r\n" + csTxt;
            }

            if (csTxt.Contains("using System.Web.Http"))
                csTxt = csTxt.Replace("using System.Web.Http", "// dnxMigrater REMOVED - using System.Web.Http");

       

            csTxt = csTxt.Replace("IHttpActionResult", "IActionResult")
                .Replace(" NotFound", " HttpNotFound")
                .Replace("Ok(", "new ObjectResult(")
                .Replace("RoutePrefix", "Route").Replace(" ActionResult "," IActionResult ");


            return csTxt;
        }

    }
}