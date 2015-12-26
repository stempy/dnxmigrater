using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Models.Dest
{
    public class StringObjectDictionay : Dictionary<string, object>
    {}

    public class ProjectDependencies : Dictionary<string, object>
    {
        public void AddDependencies(IEnumerable<ProjectReference> packagesList)
        {
            foreach (var projectReference in packagesList)
            {
                var version = projectReference.Version ?? "";
                if (!this.ContainsKey(projectReference.Name))
                    Add(projectReference.Name, version);
            }
        }
    }
}