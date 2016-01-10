using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public class FileProcessorBase
    {
        protected ILogger _log;

        public FileProcessorBase(ILogger logger)
        {
            _log = logger;
        }
    }
}