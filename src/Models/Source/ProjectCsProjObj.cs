using System;
using System.Collections.Generic;

namespace DnxMigrater.Models.Source
{
    public class ProjectCsProjObj : ICloneable
    {
        public string ProjectFilePath { get; set; }
        public string ProjectFileRelativePath { get; set; }

        public Guid ProjectGuid { get; set; }
        public string OutputType { get; set; }
        public string RootNameSpace { get; set; }
        public string AssemblyName { get; set; }
        public string TargetFrameworkVersion { get; set; }
        public string FileAlignment { get; set; }

        public IEnumerable<ProjectReference> ProjectReferences { get; set; }
        public Guid ProjectTypeGuid { get; set; }

        /// <summary>
        /// Websites have 2 project types
        /// </summary>
        public Guid ProjectTypeGuid2 { get; set; }

        public string ProjectName { get; set; }

        public ProjectType ProjectType => 
                    ProjectFilePath.EndsWith(".csproj") ? ProjectType.csproj : ProjectType.xproj;

        public IEnumerable<string> IncludeFilesList { get; set; }


        public string ToSlnProjectItemLine()
        {
            return
                $"Project(\"{ProjectTypeGuid}\") = \"{ProjectName}\", \"{ProjectFileRelativePath}\", \"{ProjectGuid}\"\nEndProject\n";
        }

        public override string ToString()
        {
            return ToSlnProjectItemLine();
        }

        public object Clone()
        {
            return new ProjectCsProjObj()
            {
                ProjectTypeGuid = this.ProjectTypeGuid,
                ProjectFileRelativePath = this.ProjectFileRelativePath,
                ProjectFilePath = this.ProjectFilePath,
                ProjectGuid = this.ProjectGuid,
                ProjectName = this.ProjectName,
                IncludeFilesList = this.IncludeFilesList,
                AssemblyName = this.AssemblyName,
                FileAlignment = this.FileAlignment,
                OutputType = this.OutputType,
                ProjectReferences = this.ProjectReferences,
                RootNameSpace = this.RootNameSpace,
                TargetFrameworkVersion = this.TargetFrameworkVersion
            };
        }
    }


}