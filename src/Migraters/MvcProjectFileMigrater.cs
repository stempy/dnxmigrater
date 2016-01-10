using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Models.Source;
using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public class MvcProjectFileMigrater : IMvcProjectFileMigrater
    {
        private readonly ILogger _log;
        private readonly IEnumerable<IFileCopyProcessor> _copyProcessors; 


        public MvcProjectFileMigrater(ILogger logger)
        {
            _log = logger;
            _copyProcessors = new IFileCopyProcessor[] {new MvcFileControllerProcessor(_log),new MvcFileRazorViewProcessor(_log) };
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

                var processor = _copyProcessors.FirstOrDefault(m => m.CanProcessFile(src));
                if (processor != null)
                {
                    processor.ProcessFile(src, dest);
                }
                else
                {
                    // standard copy
                    File.Copy(src, dest, true);
                    _log.Trace("\t\tcopied {0} --> {1}", src, dest);
                }

                //if (src.EndsWith("Controller.cs"))
                //{
                //    _log.Trace("\tprocessing controller {0} --> {1}", src, dest);
                //    var csTxt = UpdateMvcControllerFile(src);
                //    File.WriteAllText(dest, csTxt);
                //    _log.Trace("\tcontroller updated {0} --> {1}", src, dest);
                //}
                //else
                //{
                //    // standard copy
                //    File.Copy(src, dest, true);
                //    _log.Trace("\t\tcopied {0} --> {1}", src, dest);
                //}
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


            if (relativeFile.Contains("Content\\"))
            {
                // web asset
                relativeFile = relativeFile.Replace("Content\\", "wwwroot\\");
            }

            if (relativeFile.Contains("Scripts\\"))
            {
                // web asset
                relativeFile = relativeFile.Replace("Scripts\\", "wwwroot\\js\\");
            }


            if (relativeFile.Contains("fonts\\"))
            {
                // web asset
                relativeFile = relativeFile.Replace("fonts\\", "wwwroot\\fonts\\");
            }


            if (relativeFile.EndsWith(".config"))
            {
                // rename so its not picked up by project
                relativeFile = relativeFile.Replace(".config", ".config.orig");
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