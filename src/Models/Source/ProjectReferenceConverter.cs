using System.Collections.Generic;
using System.Linq;

namespace DnxMigrater.Models.Source
{
    public static class ProjectReferenceConverter
    {
        public static IDictionary<string, object> CastToDependencyDictionary(IEnumerable<ProjectReference> items)
        {
            return items.ToDictionary(x => x.Name, y => (object)y.Version);
        } 
    }
}