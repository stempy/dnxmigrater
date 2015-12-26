using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Solution
{
    public class SolutionWriter : SolutionParser, ISolutionWriter
    {
        #region [Update sln project items]
        public string UpdateProjectLine(string line, ProjectCsProjObj updateModel)
        {
            Regex projReg = new Regex(SlnProjectRegex, RegexOptions.Compiled);
            var replaced = projReg.Replace(line, SlnProjectReplace);
            return replaced;
        }

        public string UpdateSolutionProjectItems(string slnFile, IEnumerable<ProjectCsProjObj> projectUpdates)
        {
            Regex projReg = new Regex(SlnProjectRegex, RegexOptions.Compiled);
            var content = File.ReadAllText(slnFile);

            var f = projectUpdates.FirstOrDefault();
            var s = string.Format(SlnProjectReplace, f.ProjectTypeGuid, f.ProjectName, f.ProjectFileRelativePath,
                f.ProjectGuid);

            var replaced = projReg.Replace(content, s);
            return replaced;
        }
        #endregion
    }
}