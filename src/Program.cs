using System;
using System.Linq;
using DnxMigrater.Migraters;
using DnxMigrater.Other;
using NLog;

namespace DnxMigrater
{
    class Program
    {
        static void Main(string[] args)
        {
            bool copyAllProjectFiles = args.Contains("/includefiles");
            
            if (!args.Any())
            {
                Usage();
                return;
            }

            string projectsToUpgradeStr = args.FirstOrDefault(x => x.Contains("/upgrade="));
            string[] upgradeProjects = new string[] {};
            if (!string.IsNullOrEmpty(projectsToUpgradeStr))
            {
                projectsToUpgradeStr = projectsToUpgradeStr.Replace("/upgrade=", "");
                upgradeProjects = projectsToUpgradeStr.Split(',');
            }
            

            IProjectMigrater projectMigrater = ProjectMigraterFactory.CreateProjectMigrater();
            ISolutionMigrater solutionMigrater = new SolutionMigrater(projectMigrater, new NLogLogger(LogManager.GetCurrentClassLogger()));

            var srcProjectPath = args[0];
            var destDir = args.Length > 1 ? args[1] : null;
            bool isSolutionFile = srcProjectPath.EndsWith(".sln");

            if (isSolutionFile)
            {
                solutionMigrater.MigrateSolution(srcProjectPath, copyAllProjectFiles,upgradeProjects);
            }
            else
            {
                // single project
                projectMigrater.MigrateProject(srcProjectPath, false, destDir);
            }
            Console.WriteLine("Press [ENTER] to continue.");
            Console.ReadLine();
        }

        static void Usage()
        {
            Console.WriteLine("DnxMigrater\n\tUsage:\n\t\tDnxMigrater srcProjectFile|srcProjectDirectory [destdir] [/includefiles] [/upgrade=projects1,project2,...]");
            Console.WriteLine(
                 "\n\t[/includefiles] Include all files specified in .csproj files and process if MVC"
                +"\n\t[/upgrade=project1,project2] for NON Mvc Projects. Upgrade projects listed to DNX MVC 6 Approach.. Removing dependencies on System.Web, using equivalent Microsoft.AspNet.Mvc");

        }
    }
}
