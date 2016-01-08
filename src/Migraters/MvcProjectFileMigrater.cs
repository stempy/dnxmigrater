using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Models.Source;
using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public interface IMvcProjectFileMigrater
    {
        string UpdateMvcControllerFile(string src);
        void CopyMvcFiles(ProjectCsProjObj model, string srcPath, string baseSrcPath, IEnumerable<string> files, string destCopyPath);
    }

    public class MvcProjectFileMigrater : IMvcProjectFileMigrater
    {
        private ILogger _log;

        public MvcProjectFileMigrater(ILogger logger)
        {
            _log = logger;
        }



        public string UpdateMvcControllerFile(string src)
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
            if (!csTxt.Contains("Microsoft.AspNet.Mvc"))
            {
                csTxt = "using Microsoft.AspNet.Mvc;\r\n" + csTxt;
            }

            if (csTxt.Contains("using System.Web.Http"))
                csTxt = csTxt.Replace("using System.Web.Http", "// dnxMigrater REMOVED - using System.Web.Http");

            csTxt = csTxt.Replace("IHttpActionResult", "IActionResult")
                .Replace("NotFound", "HttpNotFound")
                .Replace("Ok(", "new ObjectResult(")
                .Replace("RoutePrefix", "Route");


            return csTxt;
        }

        public void CopyMvcFiles(ProjectCsProjObj model, string baseSrcPath, string destProjectJson, IEnumerable<string> filesToCopy, string destCopyPath)
        {
            var files = filesToCopy.ToList();

            // remove help page (api specific)
            files.RemoveAll(m => m.Contains("Areas\\Help"));

            foreach (var file in files)
            {
                var relativeFile = GetNewRelativeFile(file);

                // skip null new files
                if (string.IsNullOrEmpty(relativeFile))
                {
                    _log.Trace("\tSkipping file {0}",file);
                    continue;
                }
                    

                // get src and dest paths of file
                var src = Path.Combine(baseSrcPath, file);
                var dest = Path.Combine(destCopyPath, relativeFile);
                var dir = Path.GetDirectoryName(dest);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (src.EndsWith("Controller.cs"))
                {
                    _log.Trace("\tprocessing controller {0} --> {1}", src, dest);
                    var csTxt = UpdateMvcControllerFile(src);
                    File.WriteAllText(dest, csTxt);
                    _log.Trace("\tcontroller updated {0} --> {1}", src, dest);
                }
                else
                {
                    // standard copy
                    File.Copy(src, dest, true);
                    _log.Trace("\t\tcopied {0} --> {1}", src, dest);
                }
            }

            // TODO
            //make sure dest project.json has mvc dependency
            if (!model.ToProjectJsonObj().Dependencies.Any(x => x.Key.Contains("Microsoft.AspNet.Mvc")))
            {
                // not in dependencies list, so lets add it
                ((List<ProjectReference>)model.ProjectReferences).Add(new ProjectReference()
                {
                    HintPath = "packages",
                    Source = "Controller",
                    Name = "Microsoft.AspNet.Mvc"
                });

                var destProjectJsonTxt = model.ToProjectJsonObj().ToString();
                File.WriteAllText(destProjectJson, destProjectJsonTxt);
            }
        }

        /// <summary>
        /// Get new relative filename that will be appended to base dest dir path
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static string GetNewRelativeFile(string file)
        {
            // change .config file extensions
            string relativeFile = file;

            // skip helpController
            if (file.EndsWith("HelpController.cs"))
                return null;


            if (relativeFile.EndsWith(".config"))
            {
                // rename so its not picked up by project
                relativeFile = file.Replace(".config", ".config.orig");
            }

            if (relativeFile.Contains("Global.asax"))
            {
                if (relativeFile.EndsWith(".cs"))
                {
                    relativeFile = relativeFile.Replace(".cs", ".cs.orig");
                }
                else
                {
                    relativeFile.Replace("Global.asax", "Global.asax.orig");
                }
            }
            return relativeFile;
        }
    }
}