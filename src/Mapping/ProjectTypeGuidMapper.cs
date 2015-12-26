using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DnxMigrater.Mapping
{
    public class ProjectTypeGuidMapper
    {
        /// <summary>
        /// Project Type GUID in sln file
        /// .net --> dnx
        /// </summary>
        public Dictionary<Guid,Guid> CsProjToDnxDictionary = new Dictionary<Guid, Guid>()
        {
            // DotNet 4.6- class library --> DNX class library package guid
            [Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")] = Guid.Parse("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}")
        };

        public Guid UpdateGuidToNewFormat(Guid oldProjectTypeGuid)
        {
            return CsProjToDnxDictionary[oldProjectTypeGuid];
        }
    }

    public class ProjectTypes
    {
        private static Dictionary<Guid, string> _projectTypeDictionary;
        public Dictionary<Guid, string> ProjectTypeDictionary => _projectTypeDictionary;

        public ProjectTypes()
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
