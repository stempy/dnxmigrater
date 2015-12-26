namespace DnxMigrater.Migraters
{
    public interface ISolutionMigrater
    {
        void MigrateSolution(string solutionFile, bool copyAllFiles = false, string destDir = null);
    }
}