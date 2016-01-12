using DnxMigrater.Models.Source;

namespace DnxMigrater.Migraters
{
    public interface IProjectMigrater
    {
        /// <summary>
        /// Migrate .NET project (4.6 or less)
        /// .csproj
        /// packages.config
        /// app.config or web.config
        /// 
        /// to .NET DNX based project VS2015+
        /// 
        /// project.json
        /// [project].xproj
        /// appsettings.json
        /// </summary>
        /// <param name="projectFile"></param>
        /// <param name="includeFiles"></param>
        /// <param name="destDir"></param>
        ProjectCsProjObj MigrateProject(string projectFile, bool includeFiles, string destDir = null);
        ProjectCsProjObj MigrateProject(ProjectCsProjObj model, bool includeFiles, bool upgradeProjectFilesToMvc6, string destDir = null);
    }
}