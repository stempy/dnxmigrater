using System;
using DnxMigrater.Mapping;

namespace DnxMigrater.Models.Source
{
    public enum ProjectType
    {
        csproj,
        xproj
    }

    public class ProjectGuidType
    {
        public Guid ProjectTypeGuid { get; set; }
        public string GetDescription()
        {
            return "[" + ProjectTypeGuid.ToString() + "] " + new ProjectTypes().ProjectTypeDictionary[ProjectTypeGuid];
        }
    }
}