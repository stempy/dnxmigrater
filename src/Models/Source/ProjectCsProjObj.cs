using System;
using System.Collections.Generic;
using System.Linq;
using DnxMigrater.Mapping;
using DnxMigrater.Models.Dest;

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

        public ICollection<ProjectReference> ProjectReferences { get; set; }
        public Guid ProjectTypeGuid { get; set; }

        public string ProjectTypeDesc => 
                            ProjectTypesMapper.GetProjectTypeDescription(ProjectTypeGuid);

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

        /// <summary>
        /// Create projectjson object
        /// </summary>
        /// <returns></returns>
        public ProjectJsonObj ToProjectJsonObj()
        {
            // all project references from .csproj
            var projReferences = this.ProjectReferences;

            // Get .NET framework references from .csproj (anything without hintpath) --> will go in frameworks/frameworkassemblies section
            var netFrameworkReferences =
                projReferences.Where(x => x.IsFrameworkAssembly && !x.IsNugetPackage && string.IsNullOrEmpty(x.HintPath));
            var projectDependencies = projReferences.Where(x => !x.IsFrameworkAssembly);

            // references (including net framework references)
            var projectJson = new ProjectJsonObj();
            projectJson.AddDependencies(projectDependencies);
            projectJson.AddFramework("net46", netFrameworkReferences);
            //projectJson.AddFramework("dnx461", netFrameworkReferences);

            return projectJson;
        }

        public projectXProjModel ToProjectXProjObj()
        {
            return new projectXProjModel()
            {
                ProjectGuid = this.ProjectGuid,
                RootNamespace = this.RootNameSpace
            };
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

        public void SetProjectType(ProjectType projectType)
        {
            this.ProjectFilePath = this.ProjectFilePath.Replace(".csproj", ".xproj");
            this.ProjectFileRelativePath = this.ProjectFileRelativePath?.Replace(".csproj", ".xproj") ?? "";
        }
    }


}