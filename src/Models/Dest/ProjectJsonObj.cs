using System.Collections.Generic;
using System.Linq;
using DnxMigrater.Models.Source;
using Newtonsoft.Json;

namespace DnxMigrater.Models.Dest
{
    /// <summary>
    /// project.json structure for dnx projects
    /// </summary>
    public class ProjectJsonObj
    {
        [JsonProperty(PropertyName = "dependencies")]
        public ProjectDependencies Dependencies { get; set; }

        [JsonProperty(PropertyName = "frameworks")]
        public ProjectFrameworks Frameworks { get; set; }

        public ProjectJsonObj()
        {
            if (Dependencies == null) Dependencies = new ProjectDependencies();
            if (Frameworks == null) Frameworks = new ProjectFrameworks();
        }

        public void AddDependencies(IEnumerable<ProjectReference> references)
        {
            Dependencies.AddDependencies(references);
        }

        public void AddFramework(string frameworkKey, IEnumerable<ProjectReference> dependencies)
        {
            var d= new ProjectDependencies();
            d.AddDependencies(dependencies.Where(x=>!x.IsFrameworkAssembly));
            var f = new ProjectDependencies();
            f.AddDependencies(dependencies.Where(x=>x.IsFrameworkAssembly));

            Frameworks.Add(frameworkKey,new ProjectFramework() {Dependencies = d, FrameworkAssemblies = f});
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}