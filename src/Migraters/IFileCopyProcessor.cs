using System.Collections.Generic;

namespace DnxMigrater.Migraters
{
    public interface IFileCopyProcessor
    {
        bool CanProcessFile(string file);
        IDictionary<string,string> ProcessFile(string file, string dest);
    }
}