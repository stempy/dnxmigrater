using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Solution
{
    public interface ISolutionParser
    {
        /// <summary>
        /// Get Project Items from .sln file
        /// </summary>
        /// <param name="slnFile"></param>
        /// <returns></returns>
        IEnumerable<ProjectCsProjObj> ParseProjectsInSolution(string slnFile);
    }
}