using System;
using System.Collections.Generic;
using DnxMigrater.Other;

namespace DnxMigrater.Mapping
{
    public class ProjectTypeGuidMapper
    {
        private readonly ILogger _logger;

        public ProjectTypeGuidMapper(ILogger log)
        {
            _logger = log;
        }

        /// <summary>
        /// Project Type GUID in sln file
        /// .net --> dnx
        /// </summary>
        public Dictionary<Guid,Guid> CsProjToDnxDictionary = new Dictionary<Guid, Guid>()
        {
            // DotNet 4.6- class library --> DNX class library package guid
            [Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")] = Guid.Parse("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}"),

            // test
            //[Guid.Parse("{3ac096d0-a1c2-e12c-1390-a8335801fdab}")]= Guid.Parse("{3ac096d0-a1c2-e12c-1390-a8335801fdab}"),


            // MVC 5 project
            //[Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}")] = Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}"),
        };

        public Guid UpdateGuidToNewFormat(Guid oldProjectTypeGuid)
        {
            var newGuid = CsProjToDnxDictionary.ContainsKey(oldProjectTypeGuid)
                ? CsProjToDnxDictionary[oldProjectTypeGuid]
                : oldProjectTypeGuid;

           if (newGuid==oldProjectTypeGuid)
                _logger.Warn("No new guid found for project type {0}",oldProjectTypeGuid);

            return newGuid;
        }
    }
}
