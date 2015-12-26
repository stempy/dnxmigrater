using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Source
{
    public interface ICsProjectFileReader
    {
        ProjectCsProjObj ParseCsProjectFile(string projectFile, bool includeFiles);
        ProjectCsProjObj ParseCsProjectFile(ProjectCsProjObj o, bool includeFiles);
        IEnumerable<ProjectCsProjObj> PopulateFromPaths(IEnumerable<ProjectCsProjObj> items);
    }
}