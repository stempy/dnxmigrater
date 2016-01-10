using System;

namespace DnxMigrater.Models.Source
{
    public class ProjectReference : ICloneable
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
            ((string.IsNullOrEmpty(HintPath) && !Name.StartsWith("Microsoft.AspNet"))
                && (Name.StartsWith("System") || Name.StartsWith("Microsoft")));

        public string ReferenceElement { get; set; }


        public object Clone()
        {
            return new ProjectReference()
            {
                Version = this.Version,
                HintPath = this.HintPath,
                Include = this.Include,
                IsPrivate = this.IsPrivate,
                IsSpecificVersion = this.IsSpecificVersion,
                Name = this.Name,
                ReferenceElement = this.ReferenceElement,
                Source = this.Source                
            };
        }
    }
}