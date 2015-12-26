using System;
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




        public ProjectJsonObj CreateProjectJsonObj(ProjectCsProjObj projFile)
        {
            // all project references from .csproj
            var projReferences = projFile.ProjectReferences;

            // Get .NET framework references from .csproj (anything without hintpath) --> will go in frameworks/frameworkassemblies section
            var netFrameworkReferences = projReferences.Where(x => x.IsFrameworkAssembly &&  !x.IsNugetPackage && string.IsNullOrEmpty(x.HintPath));
            var projectDependencies = projReferences.Where(x => !x.IsFrameworkAssembly);

            // references (including net framework references)
            var projectJson = new ProjectJsonObj();
            projectJson.AddDependencies(projectDependencies);
            projectJson.AddFramework("net46", netFrameworkReferences);
            //projectJson.AddFramework("dnx461", netFrameworkReferences);

            return projectJson;
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
        public ProjectCsProjObj MigrateProject(string projectFile, bool includeFiles=false, string destDir=null)
        {
            projectFile = GetProjectFilePath(projectFile);
            var projectDir = Path.GetDirectoryName(projectFile);
            var projectFilename = Path.GetFileNameWithoutExtension(projectFile);
            destDir = destDir ?? Path.Combine(Path.GetTempPath()+@"\_dnx",projectFilename ?? "project");

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            _log.Debug("Migrating Project:{0} to {1}",projectFile,projectDir);

            // now process .csproj file and populate the object
            var projFileObj = _projectFileReader.ParseCsProjectFile(projectFile, includeFiles);

            var projectFileNameWithoutExt = Path.GetFileNameWithoutExtension(projFileObj.ProjectFilePath);

            // Create project.json object and appsettingsjson
            var projectJsonObj = CreateProjectJsonObj(projFileObj);
            var projectJson = projectJsonObj.ToString();
            var appSettingsJson = _appConfigMigrater.MigrateConfigToJsonAppSettings(projectDir);

            // Create xprojString
            var xprojStr =_xProjWriter.CreateXProjString(new projectXProjModel()
            {
                ProjectGuid = projFileObj.ProjectGuid,
                RootNamespace = projFileObj.RootNameSpace
            });
            

            // Write .xproj
            var destXProjFile = Path.Combine(destDir, string.Format(XprojFmt, projectFileNameWithoutExt));
            _log.Debug("Writing {0}...", destXProjFile);
            File.WriteAllText(destXProjFile, xprojStr,Encoding.UTF8);

            // update object
            projFileObj.ProjectFilePath=projFileObj.ProjectFilePath.Replace(".csproj", ".xproj");
            projFileObj.ProjectFileRelativePath = projFileObj.ProjectFileRelativePath?.Replace(".csproj", ".xproj") ??"";

            if (projFileObj.ProjectTypeGuid !=  Guid.Empty)
            {
                // update guid
                projFileObj.ProjectTypeGuid = _guidMapper.UpdateGuidToNewFormat(projFileObj.ProjectTypeGuid);
            }



            // write project.json
            if (!string.IsNullOrEmpty(projectJson))
            {
                var destprojectJsonFile = Path.Combine(destDir, ProjectJsonFile);
                _log.Debug("Writing {0}...",destprojectJsonFile);
                File.WriteAllText(destprojectJsonFile,projectJson);
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
                _log.Debug("Copying all included files from "+projectDir);
                
                // now copy all included files from project directory over to destination project directory
                var includedFiles = projFileObj.IncludeFilesList;
                var baseSrcPath = projectDir;
                var destCopyPath = destDir;

                var files = includedFiles.Where(x => !x.EndsWith("\\") && !x.Contains("*")).ToList();
                var filesPattern = includedFiles.Except(files).Where(x => x.Contains("*"));
                var folders = includedFiles.Except(files);


                foreach (var pattern in filesPattern)
                {
                    var dir = Path.GetDirectoryName(Path.Combine(baseSrcPath, pattern));
                    var f = Path.GetFileName(pattern.Replace(dir +"\\", ""));
                    var filesInDir = Directory.GetFiles(dir, f).Select(x=>x.Replace(baseSrcPath+"\\",""));
                    files.AddRange(filesInDir);
                }


                foreach (var file in files)
                {
                    var src = Path.Combine(baseSrcPath, file);
                    var dest = Path.Combine(destCopyPath, file);
                    var dir = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.Copy(src,dest,true);
                    _log.Trace("copy {0} --> {1}",src,dest);
                }
            }


            _log.Info("Migrated Project {0} Completed.",projectFileNameWithoutExt);
            return projFileObj;
        }

        public ProjectCsProjObj MigrateProject(ProjectCsProjObj model, bool includeFiles= false, string destDir = null)
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


            _log.Debug("Migrating Project: [{2}] {0}  to {1}", projectFile, projectDir,projType);

            // now process .csproj file and populate the object
            model = _projectFileReader.ParseCsProjectFile(model, includeFiles);

            var projectFileNameWithoutExt = Path.GetFileNameWithoutExtension(model.ProjectFilePath);

            // Create project.json object and appsettingsjson
            var projectJsonObj = CreateProjectJsonObj(model);
            var projectJson = projectJsonObj.ToString();
            var appSettingsJson = _appConfigMigrater.MigrateConfigToJsonAppSettings(projectDir);

            // Create xprojString
            var xprojStr = _xProjWriter.CreateXProjString(new projectXProjModel()
            {
                ProjectGuid = model.ProjectGuid,
                RootNamespace = model.RootNameSpace
            });


            // Write .xproj
            var destXProjFile = Path.Combine(destDir, string.Format(XprojFmt, projectFileNameWithoutExt));
            _log.Debug("Writing {0}...", destXProjFile);
            File.WriteAllText(destXProjFile, xprojStr, Encoding.UTF8);

            // update object
            model.ProjectFilePath = model.ProjectFilePath.Replace(".csproj", ".xproj");
            model.ProjectFileRelativePath = model.ProjectFileRelativePath?.Replace(".csproj", ".xproj") ?? "";

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
                _log.Debug("Copying all included files from " + projectDir);

                // now copy all included files from project directory over to destination project directory
                var includedFiles = model.IncludeFilesList;
                var baseSrcPath = projectDir;
                var destCopyPath = destDir;

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


                foreach (var file in files)
                {
                    var src = Path.Combine(baseSrcPath, file);
                    var dest = Path.Combine(destCopyPath, file);
                    var dir = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.Copy(src, dest, true);
                    _log.Trace("copy {0} --> {1}", src, dest);
                }
            }


            _log.Info("Migrated Project {0} Completed.", projectFileNameWithoutExt);
            return model;
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
