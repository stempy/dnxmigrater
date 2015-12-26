namespace DnxMigrater.Models.Source
{
    public class ProjectReference
    {
        //public string Name
        //{
        //    get { return Include.Contains(",")? Include.Remove(Include.IndexOf(",")):Include; }
        //}

        public string Name { get; set; }

        public string Include { get; set; }
        public string HintPath { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsSpecificVersion { get; set; }
        public string Version { get; set; }
        public string Source { get; set; }

        public bool IsNugetPackage => 
            (!string.IsNullOrEmpty(HintPath)) &&  HintPath.ToLower().Contains("packages");

        public bool IsFrameworkAssembly => 
            (string.IsNullOrEmpty(HintPath) && Name.StartsWith("System") || Name.StartsWith("Microsoft"));

        public string ReferenceElement { get; set; }
    }
}