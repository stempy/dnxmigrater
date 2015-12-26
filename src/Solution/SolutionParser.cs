using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Solution
{
    public class SolutionParser : ISolutionParser
    {
        protected const string SlnProjectRegex =
            "Project\\(\"\\{([\\w-]*)\\}\"\\) = \"([\\w _]*.*)\", \"(.*\\.(cs|vcx|vb)proj)\", \"\\{([\\w-]*)\\}\"";
        protected const string SlnProjectReplace =
            "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"";

        /// <summary>
        /// Get Project Items from .sln file
        /// </summary>
        /// <param name="slnFile"></param>
        /// <returns></returns>
        public IEnumerable<ProjectCsProjObj> ParseProjectsInSolution(string slnFile)
        {
            var Content = File.ReadAllText(slnFile);
            Regex projReg = new Regex(SlnProjectRegex, RegexOptions.Compiled);
            var matches = projReg.Matches(Content).Cast<Match>();
            var Projects = matches.Select(x => new ProjectCsProjObj()
            {
                ProjectTypeGuid = Guid.Parse(x.Groups[1].Value),
                ProjectName = x.Groups[2].Value,
                ProjectFileRelativePath = x.Groups[3].Value,
                ProjectFilePath = x.Groups[3].Value,
                ProjectGuid = Guid.Parse(x.Groups[5].Value)
            }).ToList();

            foreach (var projectCsProjObj in Projects)
            {
                var path = projectCsProjObj.ProjectFilePath;

                if (!Path.IsPathRooted(path))
                    path = Path.Combine(Path.GetDirectoryName(slnFile), path);
                path = Path.GetFullPath(path);
                projectCsProjObj.ProjectFilePath = path;
            }
            return Projects;
        }
    }
}