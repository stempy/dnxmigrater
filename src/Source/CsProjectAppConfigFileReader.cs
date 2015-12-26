using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DnxMigrater.Models.Source;
using NLog;

namespace DnxMigrater.Source
{
    /// <summary>
    /// App.config / Web.Config --> appsettings.json
    /// </summary>
    public class CsProjectAppConfigFileReader : ICsProjectAppConfigFileReader
    {
        private Logger _log = LogManager.GetCurrentClassLogger();

        public IDictionary<string, CsConfigSection> ParseConfigFile(string configFile)
        {
            var appSections = new Dictionary<string,CsConfigSection>();

            // see: http://stackoverflow.com/questions/505566/loading-custom-configuration-files
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = configFile;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            IDictionary<string, string> appSettings= new Dictionary<string, string>();
            IDictionary<string, string> connectionStrings = new Dictionary<string, string>();

            try
            {
                appSettings = config.AppSettings.Settings.AllKeys.ToDictionary(y => y,
                    y => config.AppSettings.Settings[y.ToString()].Value);

            }
            catch (Exception ex)
            {
                _log.Error(ex, "AppSettingsError-"+ex.Message);
                appSettings.Add("error",ex.Message);
            }

            try
            {
                connectionStrings = config.ConnectionStrings.ConnectionStrings.Cast<ConnectionStringSettings>()
                    .ToDictionary(x => x.Name, y => y.ConnectionString);

            }
            catch (Exception ex)
            {
                _log.Error(ex, "ConnectionStringError-" + ex.Message);
                connectionStrings.Add("error",ex.Message);
            }

            appSections.Add("AppSettings", new CsConfigSection(appSettings));
            appSections.Add("ConnectionStrings", new CsConfigSection(connectionStrings));
            return appSections;
        } 
    }
}