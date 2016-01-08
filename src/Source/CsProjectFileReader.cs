using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Source
{
    /// <summary>
    /// .csproj file reader --> project.json [projectname].xproj
    /// </summary>
    public class CsProjectFileReader : ICsProjectFileReader
    {
        private XNamespace csProjxmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        #region [Public interface]

        /// <summary>
        /// Parse csproj file
        /// </summary>
        /// <param name="projectFile"></param>
        /// <param name="includeFiles">include csproj included files from project to copy to folder</param>
        /// <returns></returns>
        //public ProjectCsProjObj ParseCsProjectFile(string projectFile, bool includeFiles)
        //{
        //    var projectDir = GetProjectDir(projectFile);
        //    projectFile = GetProjectFilePath(projectFile);
        //    var packagesConfigFile = Path.Combine(projectDir, "packages.config");

        //    var file = File.ReadAllText(projectFile);
        //    XDocument doc = XDocument.Parse(file);
        //    var o = new ProjectCsProjObj();
        //    o.ProjectFilePath = projectFile;
        //    InitObj(o, doc);
        //    var references = new List<ProjectReference>();

        //    if (!string.IsNullOrEmpty(packagesConfigFile) && File.Exists(packagesConfigFile))
        //    {
        //        var nugetRefs = GetPackageConfigReferences(packagesConfigFile);
        //        references.AddRange(nugetRefs);
        //    }

        //    var projectRefs = GetProjectReferences(doc, projectFile).Where(x => !x.IsNugetPackage);
        //    references.AddRange(projectRefs);
        //    o.ProjectReferences = references;

        //    if (includeFiles)
        //    {
        //        o.IncludeFilesList = GetProjectIncludedFiles(doc);
        //    }


        //    return o;
        //}

        /// <summary>
        /// Parse csproj file
        /// </summary>
        /// <param name="projectFile"></param>
        /// <param name="includeFiles">include csproj included files from project to copy to folder</param>
        /// <returns></returns>
        public ProjectCsProjObj ParseCsProjectFile(ProjectCsProjObj model, bool includeFiles)
        {
            var projectDir = GetProjectDir(model.ProjectFilePath);
            var projectFile = GetProjectFilePath(model.ProjectFilePath);
            var packagesConfigFile = Path.Combine(projectDir, "packages.config");
            var file = File.ReadAllText(projectFile);
            XDocument doc = XDocument.Parse(file);

            var o = (ProjectCsProjObj)model.Clone();
            o= InitObj(o, doc);
            var references = new List<ProjectReference>();

            if (!string.IsNullOrEmpty(packagesConfigFile) && File.Exists(packagesConfigFile))
            {
                var nugetRefs = GetPackageConfigReferences(packagesConfigFile);
                references.AddRange(nugetRefs);
            }

            var projectRefs = GetProjectReferences(doc, projectFile).Where(x => !x.IsNugetPackage);
            references.AddRange(projectRefs);
            o.ProjectReferences = references;

            if (includeFiles)
            {
                o.IncludeFilesList = GetProjectIncludedFiles(doc);
            }


            return o;
        }



        /// <summary>
        /// Get all included files in project
        /// ie Compile Include=""
        ///    Content Include="" 
        /// etc
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private IEnumerable<string> GetProjectIncludedFiles(XDocument doc)
        {
            var inc = new List<string>();
            var compiles= doc.Descendants(csProjxmlns + "Compile").Select(x => x.Attribute("Include")?.Value).ToArray();
            var content = doc.Descendants(csProjxmlns + "Content").Select(x => x.Attribute("Include")?.Value).ToArray();
            var none = doc.Descendants(csProjxmlns + "None").Select(x => x.Attribute("Include")?.Value).ToArray();
            var folder = doc.Descendants(csProjxmlns + "Folder").Select(x => x.Attribute("Include")?.Value).ToArray();

            inc.AddRange(compiles);
            inc.AddRange(content);
            inc.AddRange(none);
            inc.AddRange(folder);
            return inc;
        }


        public IEnumerable<ProjectCsProjObj> PopulateFromPaths(IEnumerable<ProjectCsProjObj> items)
        {
            var results = new List<ProjectCsProjObj>();
            foreach (var projectCsProjObj in results)
            {
                //results.Add(ParseCsProjectFile(projectCsProjObj.ProjectFilePath));
            }
            return results;
        } 

        #endregion

        #region [Project file basics]
        public ProjectCsProjObj InitObj(ProjectCsProjObj obj, XDocument doc)
        {
            obj.ProjectGuid = Guid.Parse(doc.Descendants(csProjxmlns + "ProjectGuid").FirstOrDefault().Value);
            var projectTypeGuids = doc.Descendants(csProjxmlns + "ProjectTypeGuids").FirstOrDefault()?.Value;
            if (projectTypeGuids != null)
            {
                // we have project type guids
                var pGuids = projectTypeGuids.Split(';');
                if (pGuids.Length > 1)
                {
                    obj.ProjectTypeGuid = Guid.Parse(pGuids[0]); // likely MVC type
                    obj.ProjectTypeGuid2 = Guid.Parse(pGuids[1]); // likely C# {fae04ec0-301f-11d3-bf4b-00c04f79efbc}
                }
            }


            obj.RootNameSpace = doc.Descendants(csProjxmlns + "RootNamespace").FirstOrDefault().Value;
            obj.AssemblyName = doc.Descendants(csProjxmlns + "AssemblyName").FirstOrDefault().Value;
            obj.TargetFrameworkVersion = doc.Descendants(csProjxmlns + "TargetFrameworkVersion").FirstOrDefault().Value;
            obj.OutputType = doc.Descendants(csProjxmlns + "OutputType").FirstOrDefault().Value;

            return obj;
        }
        #endregion


        #region [Get Project and Package References]

        /// <summary>
        /// from .csproj
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public IEnumerable<ProjectReference> GetProjectReferences(XDocument doc, string file)
        {
            var packages = new List<ProjectReference>();
            var source = Path.GetFileName(file);
            var references = GetCsProjectReferences(doc, source, "Reference");
            var projReferences = GetCsProjectReferences(doc, source, "ProjectReference");
            packages.AddRange(references);
            packages.AddRange(projReferences);
            return packages;
        }

        private IEnumerable<ProjectReference> GetCsProjectReferences(XDocument doc, string source, string referenceElement)
        {
            List<ProjectReference> packages = new List<ProjectReference>();

            IEnumerable<XElement> projReferences =
                from el in doc.Descendants(csProjxmlns + referenceElement)
                select el;
            foreach (XElement e in projReferences)
            {
                var inc = e.Attribute("Include").Value;
                var subName = e.Element(csProjxmlns+"Name")?.Value;
                var name = subName ?? (  inc.Contains(",") ? inc.Remove(inc.IndexOf(",")) : inc);
                var hintPath = e.Elements(csProjxmlns + "HintPath").FirstOrDefault();
                var specificVersion = e.Elements(csProjxmlns + "SpecificVersion").FirstOrDefault();
                var isPrivate = e.Elements(csProjxmlns + "Private").FirstOrDefault() != null;
                packages.Add(new ProjectReference()
                {
                    ReferenceElement = referenceElement,
                    Name = name,
                    Source = source,
                    Include = inc,
                    HintPath = hintPath?.Value,
                    IsPrivate = isPrivate,
                    IsSpecificVersion = specificVersion != null && (specificVersion.Value == "True" ? true : false)
                });
            }

            return packages;
        }


        /// <summary>
        /// from packages.config nuget
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public IEnumerable<ProjectReference> GetPackageConfigReferences(string filename)
        {
            var packages = new List<ProjectReference>();
            var source = Path.GetFileName(filename);
            XDocument doc = XDocument.Load(filename);
            IEnumerable<XElement> childList =
                from el in doc.Elements().FirstOrDefault(x => x.Name == "packages").Elements()
                select el;
            foreach (XElement e in childList)
            {
                var name = e.Attribute("id").Value;
                var version = e.Attribute("version").Value;
                packages.Add(new ProjectReference()
                {
                    Source = source,
                    Name = name,
                    Version = version,
                    HintPath = "packages.config"
                });
            }
            return packages;
        }

        #endregion

        #region [Get Paths]

        private string GetProjectDir(string projectPath)
        {
            if (Directory.Exists(projectPath))
                return projectPath;

            if (File.Exists(projectPath))
                return Path.GetDirectoryName(projectPath);


            throw new DirectoryNotFoundException("unable to find project path for " + projectPath);
        }

        private string GetProjectFilePath(string projectPath)
        {
            string projectFile = null;
            if (File.Exists(projectPath))
            {
                projectFile = projectPath;
            }
            else
            {
                // check directory
                var files = Directory.GetFiles(projectPath, "*.csproj").ToArray();
                if (!files.Any())
                    throw new FileNotFoundException("unable to find project file in " + projectPath);

                if (files.Length > 1)
                    throw new Exception("Ambiguous .csproj file list in " + projectPath);

                projectFile = files.FirstOrDefault();

            }
            return projectFile;
        }

        #endregion

    }
}