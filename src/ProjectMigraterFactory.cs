using DnxMigrater.Migraters;
using DnxMigrater.Other;
using DnxMigrater.Source;
using NLog;

namespace DnxMigrater
{
    public static class ProjectMigraterFactory
    {
        public static IProjectMigrater CreateProjectMigrater()
        {
            ITemplateRenderer templateRenderer = new MustacheRenderer();
            ICsProjectFileReader projectFileReader = new CsProjectFileReader();
            ICsProjectAppConfigFileReader appConfigFileReader = new CsProjectAppConfigFileReader();
            IAppConfigToJsonAppSettingsMigrater appConfigToJsonAppSettingsMigrater = new AppConfigToJsonAppSettingsMigrater(appConfigFileReader);

            // TODO: This gets this logger, not class its logging from :( need to get correct one for class
            // ie logger could be passed into any class and it logs as type passed in here, OR default one
            var logger = new NLogLogger(LogManager.GetCurrentClassLogger());
            IProjectMigrater projectMigrater = new ProjectMigrater(projectFileReader, appConfigToJsonAppSettingsMigrater,templateRenderer, logger);
            return projectMigrater;
        }
    }
}