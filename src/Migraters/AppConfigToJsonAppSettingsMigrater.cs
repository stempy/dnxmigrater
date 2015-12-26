using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnxMigrater.Models.Source;
using DnxMigrater.Source;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DnxMigrater.Migraters
{
    public class AppConfigToJsonAppSettingsMigrater : IAppConfigToJsonAppSettingsMigrater
    {
        private readonly ICsProjectAppConfigFileReader _cfgFileReader;

        public AppConfigToJsonAppSettingsMigrater(ICsProjectAppConfigFileReader cfgFileReader)
        {
            _cfgFileReader = cfgFileReader;
        }


        public IDictionary<string, CsConfigSection> ParseConfig(string configFile)
        {
            return _cfgFileReader.ParseConfigFile(configFile);
        }

        public string MigrateConfigToJsonAppSettings(string configPath)
        {
            string configFile = GetValidAppOrWebConfigFilePath(configPath);
            string jsonAppSettings=null;
            if (!string.IsNullOrEmpty(configFile))
            {
                jsonAppSettings = CreateAppSettingsJson(configFile);
            }
            return jsonAppSettings;
        }

        public string GetValidAppOrWebConfigFilePath(string configPath)
        {
            string configFile = null;
            if (File.Exists(configPath))
            {
                configFile = configPath;
            }
            else
            {
                if (File.Exists(Path.Combine(configPath, "app.config")))
                {
                    configFile = Path.Combine(configPath, "app.config");
                }
                else if (File.Exists(Path.Combine(configPath, "web.config")))
                {
                    configFile = Path.Combine(configPath, "web.config");
                }
            }
            return configFile;
        }

        /// <summary>
        /// AppSettings and ConnectionStrings to appsettings.json
        /// from web.config or app.config file
        /// </summary>
        /// <param name="srcConfigFile"></param>
        /// <returns></returns>
        private string CreateAppSettingsJson(string srcConfigFile)
        {
            var sections = ParseConfig(srcConfigFile);
            var jo = new JObject();
            // appsettings

            var appSettingsObj = new JObject();
            foreach (var source in sections.FirstOrDefault(x => x.Key == "AppSettings").Value)
            {
                var key = source.Key;
                var val = source.Value;
                appSettingsObj.Add(new JProperty(key, val));
            }
            var appSettings = new JProperty("AppSettings", appSettingsObj);
            jo.Add(appSettings);


            // connectionstrings
            var cs = new JObject();
            foreach (var source in sections.FirstOrDefault(x => x.Key == "ConnectionStrings").Value)
            {
                var key = source.Key;
                var val = source.Value;

                var conStr = new JProperty(key, new JObject(
                    new JProperty("ConnectionString", val)));
                cs.Add(conStr);
            }

            jo.Add(new JProperty("Data", cs));


            return jo.ToString(Formatting.Indented);
        }


    }
}