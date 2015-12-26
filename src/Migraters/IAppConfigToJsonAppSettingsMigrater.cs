using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Migraters
{
    public interface IAppConfigToJsonAppSettingsMigrater
    {
        IDictionary<string, CsConfigSection> ParseConfig(string configFile);

        string MigrateConfigToJsonAppSettings(string configPath);
        string GetValidAppOrWebConfigFilePath(string configPath);

    }
}