﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Mapping;
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
        private readonly IMvcProjectFileMigrater _mvcProjectFileMigrater;

        private readonly IFileCopyProcessor _mvc6FileUpgrader;

        private XProjWriter _xProjWriter;
        private readonly ProjectTypeGuidMapper _guidMapper;
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
            _guidMapper = new ProjectTypeGuidMapper(_log);
            _mvcProjectFileMigrater = new MvcProjectFileMigrater(_log);
            _mvc6FileUpgrader = new Mvc6FileUpgrader(_log);
        }
        #endregion

        #region [Migrate Interface]
        public ProjectCsProjObj MigrateProject(string projectFile, bool includeFiles, string destDir = null)
        {
            return MigrateProject(new ProjectCsProjObj() {ProjectFilePath = projectFile},includeFiles,true, destDir);
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
        /// <param name="model"></param>
        /// <param name="includeFiles">Include all included files from csproj file</param>
        /// <param name="upgradeProjectFilesToMvc6"></param>
        /// <param name="destDir"></param>
        /// <param name="projectFile"></param>
        public ProjectCsProjObj MigrateProject(ProjectCsProjObj model, bool includeFiles = false, bool upgradeProjectFilesToMvc6 = false, string destDir = null)
        {
            var projectFile = GetProjectFilePath(model.ProjectFilePath);
            var projectFileNameWithoutExt = Path.GetFileNameWithoutExtension(model.ProjectFilePath);
            var projectDir = Path.GetDirectoryName(projectFile);
            destDir = destDir ?? Path.Combine(Path.GetTempPath() + @"\_dnx", projectFileNameWithoutExt ?? "project");

            // now process .csproj file and populate the object
            model = _projectFileReader.ParseCsProjectFile(model, includeFiles);
            var projDetails =
                $" --- Project: {model.ProjectName} Type:{model.ProjectTypeDesc} Framework:{model.TargetFrameworkVersion} ---";

            _log.Info(projDetails);
            _log.Debug("Source: {0} -->  {1}", projectFile, projectDir);

            // where to output new project based files
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            // 1.Write .xproj
            var destXProjFile = Path.Combine(destDir, string.Format(XprojFmt, projectFileNameWithoutExt));
            _log.Debug("\tWriting {0}...", Path.GetFileName(destXProjFile));
            _xProjWriter.WriteXProjFile(model.ToProjectXProjObj(), destXProjFile);
            model.SetProjectType(ProjectType.xproj);
            if (model.ProjectTypeGuid != Guid.Empty)
            {
                // update guid in object
                model.ProjectTypeGuid = _guidMapper.UpdateGuidToNewFormat(model.ProjectTypeGuid);
            }

            // 2.Create project.json object
            var projectJson = model.ToProjectJsonObj().ToString();
            var destprojectJsonFile = Path.Combine(destDir, ProjectJsonFile);
            if (!string.IsNullOrEmpty(projectJson))
            {
                _log.Debug("\tWriting {0}...", Path.GetFileName(destprojectJsonFile));
                File.WriteAllText(destprojectJsonFile, projectJson);
            }

            // 3. Create app.settings JSON string from app.config/web.config
            var appSettingsJson = _appConfigMigrater.MigrateConfigToJsonAppSettings(projectDir);
            if (!string.IsNullOrEmpty(appSettingsJson))
            {
                var destAppSettingsFile = Path.Combine(destDir, AppSettingsJsonFile);
                _log.Debug("\tMigrating {0} to {1}", projectDir, Path.GetFileName(destAppSettingsFile));
                File.WriteAllText(destAppSettingsFile, appSettingsJson);
            }

            // copy ALL csproj included files to destination
            if (includeFiles)
            {
                CopyIncludedProjectFiles(model, destDir, projectDir, destprojectJsonFile, upgradeProjectFilesToMvc6);
            }

            _log.Info(" --- Project: {0} Completed to \"{1}\" ---", projectFileNameWithoutExt, destDir);
            return model;
        }

        #endregion

        /// <summary>
        /// Copy files included in project to dest
        /// </summary>
        /// <param name="model"></param>
        /// <param name="destDir"></param>
        /// <param name="projectSrcDir"></param>
        /// <param name="destProjectJson"></param>
        /// <param name="upgradeProjectFiles"></param>
        private void CopyIncludedProjectFiles(ProjectCsProjObj model, string destDir, string projectSrcDir, string destProjectJson, bool upgradeProjectFiles)
        {
            _log.Debug("Copying all included files from " + projectSrcDir);

            // now copy all included files from project directory over to destination project directory
            var includedFiles = model.IncludeFilesList;
            var baseSrcPath = projectSrcDir;
            var destCopyPath = destDir;
            var files = includedFiles.Where(x => !x.EndsWith("\\") && !x.Contains("*")).ToList();
            var filesPattern = includedFiles.Except(files).Where(x => x.Contains("*"));
            var folders = includedFiles.Except(files);
            var isMvcBasedProject = model.ProjectTypeDesc !=null && model.ProjectTypeDesc.Contains("MVC");

            foreach (var pattern in filesPattern)
            {
                var dir = Path.GetDirectoryName(Path.Combine(baseSrcPath, pattern));
                var f = Path.GetFileName(pattern.Replace(dir + "\\", ""));
                var filesInDir = Directory.GetFiles(dir, f).Select(x => x.Replace(baseSrcPath + "\\", ""));
                files.AddRange(filesInDir);
            }

            if (isMvcBasedProject)
            {
                // process mvc based projects, many more changes to implement
                _mvcProjectFileMigrater.CopyMvcFiles(model, baseSrcPath, destProjectJson, files, destCopyPath);
                return;
            } 

            var newProjectDependenciesToAdd =new Dictionary<string,string>();

            foreach (var file in files)
            {
                var relativeFile = file;

                // change .config file extensions
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

                var src = Path.Combine(baseSrcPath, file);
                var dest = Path.Combine(destCopyPath, relativeFile);
                var dir = Path.GetDirectoryName(dest);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);


                if (upgradeProjectFiles && _mvc6FileUpgrader.CanProcessFile(src))
                {
                    var newProjectDepenencies= _mvc6FileUpgrader.ProcessFile(src, dest);
                    if (newProjectDepenencies!=null)
                        foreach (var newProjectDepenency in newProjectDepenencies)
                        {
                            if (!newProjectDependenciesToAdd.ContainsKey(newProjectDepenency.Key))
                                newProjectDependenciesToAdd.Add(newProjectDepenency.Key,newProjectDepenency.Value);
                        }
                }
                else
                {
                    File.Copy(src, dest, true);
                    _log.Trace("copy {0} --> {1}", src, dest);
                }
            }

            if (newProjectDependenciesToAdd.Any())
            {
                UpdateProjectDependencies(model,newProjectDependenciesToAdd,destProjectJson);
                newProjectDependenciesToAdd = new Dictionary<string, string>();
            }
        }

        private void UpdateProjectDependencies(ProjectCsProjObj model, IDictionary<string, string> dependencies, string destProjJsonFile)
        {
            var refList = model.ProjectReferences.ToList();

            _log.Warn("Removing System.Web dependencies...");

            // Remove System.Web Dependencies
            refList.RemoveAll(x => x.Name.StartsWith("System.Web"));


            refList.AddRange(dependencies.Select(x => new ProjectReference()
            {
                Name = x.Key,
                Version = x.Value,
                HintPath = "packages"
            }));
            model.ProjectReferences = refList;
            var destProjectObj = model.ToProjectJsonObj();
            File.WriteAllText(destProjJsonFile, destProjectObj.ToString());
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
