using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DnxMigrater.Mapping;
using DnxMigrater.Models.Dest;
using DnxMigrater.Models.Source;
using DnxMigrater.Other;
using DnxMigrater.Source;

namespace DnxMigrater.Migraters
{
    /// <summary>
    /// Main Project Migrater
    /// </summary>
    public class ProjectMigrater : IProjectMigrater
    {
        #region [Flds]

        private const string ProjectJsonFile = "project.json";
        private const string AppSettingsJsonFile = "appsettings.json";
        private const string XprojFmt = "{0}.xproj";
        private readonly IAppConfigToJsonAppSettingsMigrater _appConfigMigrater;
        private readonly ILogger _log;
        private readonly ITemplateRenderer _templateRenderer;
        private readonly ICsProjectFileReader _projectFileReader;

        private XProjWriter _xProjWriter;
        private readonly ProjectTypeGuidMapper _guidMapper;
        private ProjectTypes _projectTypes;

        #endregion

        #region [Ctors]

        public ProjectMigrater(ICsProjectFileReader projectFileReader,
            IAppConfigToJsonAppSettingsMigrater appConfigMigrater,
            ITemplateRenderer templateRenderer,
            ILogger logger)
        {
            _appConfigMigrater = appConfigMigrater;
            _log = logger;
            _templateRenderer = templateRenderer;
            _projectFileReader = projectFileReader;
            _xProjWriter = new XProjWriter(_templateRenderer);
            _guidMapper = new ProjectTypeGuidMapper();
            _projectTypes = new ProjectTypes();
        }


        #endregion

        #region [Migrate Interface]
        public ProjectCsProjObj MigrateProject(string projectFile, bool includeFiles, string destDir = null)
        {
            return MigrateProject(new ProjectCsProjObj() {ProjectFilePath = projectFile},includeFiles,destDir);
        }
      
        /// <summary>
        /// Migrate .NET project (4.6 or less)
        /// .csproj
        /// packages.config
        /// app.config or web.config
        /// 
        /// to .NET DNX based project VS2015+
        /// 
        /// project.json
        /// [project].xproj
        /// appsettings.json
        /// </summary>
        /// <param name="projectFile"></param>
        /// <param name="includeFiles">Include all included files from csproj file</param>
        /// <param name="destDir"></param>
        public ProjectCsProjObj MigrateProject(ProjectCsProjObj model, bool includeFiles = false, string destDir = null)
        {
            var projectFile = model.ProjectFilePath;

            projectFile = GetProjectFilePath(projectFile);
            var projectDir = Path.GetDirectoryName(projectFile);
            var projectFilename = Path.GetFileNameWithoutExtension(projectFile);
            destDir = destDir ?? Path.Combine(Path.GetTempPath() + @"\_dnx", projectFilename ?? "project");

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            string projType = "";

            if (model.ProjectTypeGuid != Guid.Empty)
            {
                projType = _projectTypes.ProjectTypeDictionary[model.ProjectTypeGuid];
            }

            _log.Debug("Migrating Project: [{2}] {0}  to {1}", projectFile, projectDir, projType);

            // now process .csproj file and populate the object
            model = _projectFileReader.ParseCsProjectFile(model, includeFiles);

            var projectFileNameWithoutExt = Path.GetFileNameWithoutExtension(model.ProjectFilePath);

            // Create project.json object and appsettingsjson
            var projectJson = model.ToProjectJsonObj().ToString();
            var appSettingsJson = _appConfigMigrater.MigrateConfigToJsonAppSettings(projectDir);

            // Write .xproj
            var destXProjFile = Path.Combine(destDir, string.Format(XprojFmt, projectFileNameWithoutExt));
            _log.Debug("Writing {0}...", destXProjFile);
            _xProjWriter.WriteXProjFile(model.ToProjectXProjObj(),destXProjFile);

            // update project type to xproj
            model.SetProjectType(ProjectType.xproj);

            if (model.ProjectTypeGuid != Guid.Empty)
            {
                // update guid
                model.ProjectTypeGuid = _guidMapper.UpdateGuidToNewFormat(model.ProjectTypeGuid);
            }

            // write project.json
            if (!string.IsNullOrEmpty(projectJson))
            {
                var destprojectJsonFile = Path.Combine(destDir, ProjectJsonFile);
                _log.Debug("Writing {0}...", destprojectJsonFile);
                File.WriteAllText(destprojectJsonFile, projectJson);
            }

            // write appsettings.json if there was an app.config or web.config
            if (!string.IsNullOrEmpty(appSettingsJson))
            {
                var destAppSettingsFile = Path.Combine(destDir, AppSettingsJsonFile);
                _log.Debug("Migrating {0} to {1}", projectDir, destAppSettingsFile);
                File.WriteAllText(destAppSettingsFile, appSettingsJson);
            }

            if (includeFiles)
            {
                CopyIncludedProjectFiles(model, destDir, projectDir);
            }


            _log.Info("Migrated Project {0} Completed.", projectFileNameWithoutExt);
            return model;
        }

        #endregion

        /// <summary>
        /// Copy files included in project to dest
        /// </summary>
        /// <param name="model"></param>
        /// <param name="destDir"></param>
        /// <param name="projectDir"></param>
        private void CopyIncludedProjectFiles(ProjectCsProjObj model, string destDir, string projectDir)
        {
            _log.Debug("Copying all included files from " + projectDir);

            // now copy all included files from project directory over to destination project directory
            var includedFiles = model.IncludeFilesList;
            var baseSrcPath = projectDir;
            var destCopyPath = destDir;
            var destProjectJson = Path.Combine(destCopyPath, ProjectJsonFile);



            var files = includedFiles.Where(x => !x.EndsWith("\\") && !x.Contains("*")).ToList();
            var filesPattern = includedFiles.Except(files).Where(x => x.Contains("*"));
            var folders = includedFiles.Except(files);


            foreach (var pattern in filesPattern)
            {
                var dir = Path.GetDirectoryName(Path.Combine(baseSrcPath, pattern));
                var f = Path.GetFileName(pattern.Replace(dir + "\\", ""));
                var filesInDir = Directory.GetFiles(dir, f).Select(x => x.Replace(baseSrcPath + "\\", ""));
                files.AddRange(filesInDir);
            }

            bool IsMvcBasedProject = false;

            // remove help page (api specific)
            files.RemoveAll(m => m.Contains("Areas\\Help"));

            foreach (var file in files)
            {
                var relativeFile = file;

                // change .config file extensions
                if (relativeFile.EndsWith(".config"))
                {
                    // rename so its not picked up by project
                    relativeFile = file.Replace(".config", ".config.orig");
                }

                if (file.EndsWith("HelpController.cs"))
                    continue;


                var src = Path.Combine(baseSrcPath, file);
                var dest = Path.Combine(destCopyPath, relativeFile);
                var dir = Path.GetDirectoryName(dest);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (src.EndsWith("Controller.cs"))
                {
                    _log.Trace("processing controller {0} --> {1}", src, dest);
                    var csTxt = UpdateMvcControllerFile(src);
                    File.WriteAllText(dest, csTxt);
                    _log.Trace("controller updated {0} --> {1}", src, dest);
                    IsMvcBasedProject = true;
                }
                else
                {
                    File.Copy(src, dest, true);
                    _log.Trace("copy {0} --> {1}", src, dest);
                }
            }

            if (IsMvcBasedProject)
            {
                // TODO
                //make sure dest project.json has mvc dependency
                if (!model.ToProjectJsonObj().Dependencies.Any(x => x.Key.Contains("Microsoft.AspNet.Mvc")))
                {
                    // not in dependencies list, so lets add it
                    ((List<ProjectReference>) model.ProjectReferences).Add(new ProjectReference()
                    {
                        HintPath = "packages",
                        Source = "Controller",
                        Name = "Microsoft.AspNet.Mvc"
                    });

                    var destProjectJsonTxt = model.ToProjectJsonObj().ToString();
                    var destprojectJsonFile = Path.Combine(destDir, ProjectJsonFile);
                    File.WriteAllText(destprojectJsonFile,destProjectJsonTxt);
                }

               
                //if (!destProjectJsonTxt.Contains("Micro"))
            }
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
            if (!csTxt.Contains("Microsoft.AspNet.Mvc"))
            {
                csTxt = "using Microsoft.AspNet.Mvc;\r\n" + csTxt;
            }

            if (csTxt.Contains("using System.Web.Http"))
                csTxt = csTxt.Replace("using System.Web.Http", "// dnxMigrater REMOVED - using System.Web.Http");

            csTxt = csTxt.Replace("IHttpActionResult", "IActionResult")
                         .Replace("NotFound","HttpNotFound")
                         .Replace("Ok(","new ObjectResult(")
                         .Replace("RoutePrefix","Route");
            

            return csTxt;
        }


        private string GetProjectFilePath(string projectPath)
        {
            string projectFile = null;
            if (File.Exists(projectPath))
            {
                projectFile = projectPath;
            }
            else
            {
                // check directory
                var files = Directory.GetFiles(projectPath, "*.csproj").ToArray();
                if (!files.Any())
                    throw new FileNotFoundException("unable to find project file in "+projectPath);

                if (files.Length > 1)
                    throw new Exception("Ambiguous .csproj file list in "+projectPath);

                projectFile = files.FirstOrDefault();

            }
            return projectFile;
        }

    }
}
