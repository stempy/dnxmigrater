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

            IProjectMigrater projectMigrater = ProjectMigraterFactory.CreateProjectMigrater();
            ISolutionMigrater solutionMigrater = new SolutionMigrater(projectMigrater, new NLogLogger(LogManager.GetCurrentClassLogger()));

            var srcProjectPath = args[0];
            var destDir = args.Length > 1 ? args[1] : null;
            bool isSolutionFile = srcProjectPath.EndsWith(".sln");

            if (isSolutionFile)
            {
                solutionMigrater.MigrateSolution(srcProjectPath, copyAllProjectFiles);
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
            Console.WriteLine("DotNetDnxProjectMigrater\n\tUsage:\n\t\tDotNetToDnxProjectMigrater srcProjectFile|srcProjectDirectory [destdir]");
        }
    }
}
