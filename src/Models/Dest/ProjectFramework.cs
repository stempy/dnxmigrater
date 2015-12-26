using Newtonsoft.Json;

namespace DnxMigrater.Models.Dest
{
    public class ProjectFramework
    {
        [JsonProperty(PropertyName = "dependencies")]
        public ProjectDependencies Dependencies { get; set; }

        [JsonProperty(PropertyName = "frameworkAssemblies")]
        public ProjectDependencies FrameworkAssemblies { get; set; }

    }
}