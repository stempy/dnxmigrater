using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Mapping;
using DnxMigrater.Models.Dest;
using DnxMigrater.Models.Source;
using DnxMigrater.Solution;
using NLog;
using NLog.Targets;
using ILogger = DnxMigrater.Other.ILogger;

namespace DnxMigrater.Migraters
{
    /// <summary>
    /// Update all projects within a solution
    /// </summary>
    public class SolutionMigrater : ISolutionMigrater
    {
        #region [Flds]
        private readonly IProjectMigrater _projectMigrater;
        private readonly SolutionParser _solutionParser;
        private readonly ILogger _log;
        private ProjectTypeGuidMapper _guidMapper;

        #endregion

        #region [Ctor]
        public SolutionMigrater(IProjectMigrater projectMigrater, ILogger logger)
        {
            _projectMigrater = projectMigrater;
            _solutionParser = new SolutionParser();
            _log = logger;
            _guidMapper = new ProjectTypeGuidMapper(_log);
        }
        #endregion

        public void MigrateSolution(string solutionFile, bool copyAllFiles, string[] upgradeProjects, string destDir = null)
        {
            if (!File.Exists(solutionFile))
            {
                throw new FileNotFoundException("solution file "+solutionFile +" not found");
            }

            var slnFileNameOnly = Path.GetFileName(solutionFile);
            var tmpDir = destDir ?? Path.Combine(Path.GetTempPath(), "_dnxSolution\\" + slnFileNameOnly);

            // --------------- NLOG specific code to set log file name ------------------------------------
            // TODO: 26/12/2015 logger is abstracted yet using direct NLOG methods atm to set logfile path
            var target = (FileTarget)LogManager.Configuration.FindTargetByName("f");
            target.FileName = $"{tmpDir}/{slnFileNameOnly}_migration.log";
            LogManager.ReconfigExistingLoggers();
            // --------------- NLOG specific code to set log file name ------------------------------------

            _log.Info("Migrating solution file {0}",solutionFile);


            var newSrcDir = Path.Combine(tmpDir, "src");
            _log.Debug("Debug Dir {0}", tmpDir);
            _log.Debug("Analyzing Projects in solution...");
            var projectItems = _solutionParser.ParseProjectsInSolution(solutionFile);
            _log.Debug("Found {0} projects\nDebug Dir {1}", projectItems.Count(),tmpDir);
            int updateCount = 0;

            var updatedProjects = new List<ProjectCsProjObj>();
            foreach (var projectCsProjObj in projectItems)
            {
                var destProjDir = Path.Combine(newSrcDir, projectCsProjObj.ProjectName);
                bool upgradeThisProj = upgradeProjects.Any(x => x == projectCsProjObj.ProjectName);
                _log.Debug("Migrating Project {2} {0} to {1}",projectCsProjObj.ProjectName,destProjDir,upgradeThisProj?"[DNXUPGRADE]":"");
                
                var updatedProj = _projectMigrater.MigrateProject(projectCsProjObj,copyAllFiles, upgradeThisProj, destProjDir);

                // fix up relative path
                var newPathDir = Path.GetDirectoryName(updatedProj.ProjectFileRelativePath);
                //var newPath = "src\\" + newPathDir.Substring(newPathDir.IndexOf(Path.GetFileName(newPathDir)));

                var newPath = "src\\" + updatedProj.ProjectName;

                newPath = Path.Combine(newPath, Path.GetFileName(updatedProj.ProjectFileRelativePath));
                updatedProj.ProjectFileRelativePath = newPath;
                updatedProj.ProjectTypeGuid = _guidMapper.UpdateGuidToNewFormat(projectCsProjObj.ProjectTypeGuid);
                updatedProjects.Add(updatedProj);
                updateCount++;
            }

            if (updateCount != projectItems.Count())
            {
                _log.Error("Updated item count dont match update:{0} total:{1}",updateCount,projectItems.Count());
            }


            // ------------ update solution file with new types, paths guids ------------------------
            
            var newSlnFile = Path.Combine(tmpDir, Path.GetFileName(solutionFile));
            _log.Debug("Writing {0}",newSlnFile);
            
            // copy original
            File.Copy(solutionFile, Path.Combine(tmpDir,Path.GetFileNameWithoutExtension(solutionFile)+".sln.orig"),true);
            // copy, than update solution, CRUDE but better than nothing
            File.Copy(solutionFile, newSlnFile,true);

            var newSlnTxt = File.ReadAllText(newSlnFile);
            foreach (var updatedProj in updatedProjects)
            {
                var origProj = projectItems.FirstOrDefault(x => x.ProjectGuid == updatedProj.ProjectGuid);
                newSlnTxt= newSlnTxt.Replace(origProj.ProjectFileRelativePath, updatedProj.ProjectFileRelativePath);
                if (updatedProj.ProjectTypeGuid != origProj.ProjectTypeGuid)
                    newSlnTxt = newSlnTxt.Replace(origProj.ProjectTypeGuid.ToString().ToUpper(), updatedProj.ProjectTypeGuid.ToString().ToUpper());
            }

            // globaljson file reference
            var firstProjectIdx = newSlnTxt.IndexOf("Project(\"");
            var globalJson = new GlobalJsonObj() {Sources = new string[] {"src"}};
            newSlnTxt=newSlnTxt.Insert(firstProjectIdx, globalJson.GetGlobalJsonReferenceForSln());

            // write updated sln file
            File.WriteAllText(newSlnFile,newSlnTxt);
            // ------------ update solution file with new types, paths guids ------------------------

            // write other stuff
            File.WriteAllText(Path.Combine(tmpDir, "global.json"),globalJson.ToString());

            _log.Info("Migrated solution {0} [{2} projects updated]  --> {1}",solutionFile,tmpDir,updateCount);
        }
    }
}