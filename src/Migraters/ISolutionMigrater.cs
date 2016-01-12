namespace DnxMigrater.Migraters
{
    public interface ISolutionMigrater
    {
        void MigrateSolution(string solutionFile, bool copyAllFiles, string[] upgradeProjects, string destDir = null);
    }
}