using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Source
{
    public interface ICsProjectAppConfigFileReader
    {
        IDictionary<string, CsConfigSection> ParseConfigFile(string configFile);
    }
}