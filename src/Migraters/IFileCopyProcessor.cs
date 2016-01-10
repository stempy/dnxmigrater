namespace DnxMigrater.Migraters
{
    public interface IFileCopyProcessor
    {
        bool CanProcessFile(string file);
        void ProcessFile(string file, string dest);
    }
}