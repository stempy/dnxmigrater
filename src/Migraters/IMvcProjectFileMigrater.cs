using System.Collections.Generic;
using DnxMigrater.Models.Source;

namespace DnxMigrater.Migraters
{
    public interface IMvcProjectFileMigrater
    {
        void CopyMvcFiles(ProjectCsProjObj model, string srcPath, string baseSrcPath, IEnumerable<string> files, string destCopyPath);
    }
}