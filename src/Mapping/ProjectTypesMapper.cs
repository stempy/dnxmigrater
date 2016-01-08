using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DnxMigrater.Mapping
{
    public static class ProjectTypesMapper
    {
        private static Dictionary<Guid, string> _projectTypeDictionary;
        

        public static string GetProjectTypeDescription(Guid projectTypeGuid)
        {
            return _projectTypeDictionary.ContainsKey(projectTypeGuid)? _projectTypeDictionary[projectTypeGuid]:null;
        }

        public static Guid GetProjectTypeGuid(string projectTypeString)
        {
            return _projectTypeDictionary.ContainsValue(projectTypeString)
                ? _projectTypeDictionary.FirstOrDefault(x => x.Value == projectTypeString).Key
                : Guid.Empty;
        }


        static ProjectTypesMapper()
        {
            if (_projectTypeDictionary == null)
            {
                _projectTypeDictionary = new Dictionary<Guid, string>();
                var ptypesContent = File.ReadLines(@"visual_studio_project_type_guids_list.csv");
                foreach (var line in ptypesContent)
                {
                    var lineArr = line.Split(',');
                    var desc = lineArr[0];
                    var guidStr = lineArr[1];
                    if (!string.IsNullOrEmpty(guidStr))
                    {
                        var g = Guid.Parse(guidStr);
                        if (!_projectTypeDictionary.ContainsKey(g))
                            _projectTypeDictionary.Add(g,desc);
                    }
                }
            }
        }
    }
}