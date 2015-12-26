using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Solution
{
    public interface ISolutionWriter
    {
        string UpdateProjectLine(string line, ProjectCsProjObj updateModel);
        string UpdateSolutionProjectItems(string slnFile, IEnumerable<ProjectCsProjObj> projectUpdates);
    }
}