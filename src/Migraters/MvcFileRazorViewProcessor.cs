using System.Collections.Generic;
using System.IO;
using DnxMigrater.Other;

namespace DnxMigrater.Migraters
{
    public class MvcFileRazorViewProcessor : FileProcessorBase,  IFileCopyProcessor
    {
        public MvcFileRazorViewProcessor(ILogger logger) : base(logger)
        {
        }

        public bool CanProcessFile(string file)
        {
            return file.EndsWith(".cshtml");
        }

        public IDictionary<string,string> ProcessFile(string file, string dest)
        {
            _log.Trace("\tprocessing razor view {0} --> {1}", file, dest);
            var csTxt = UpdateRazorView(file);
            File.WriteAllText(dest, csTxt);
            _log.Trace("\trazor updated {0} --> {1}", file, dest);
            return null;
        }

        private string UpdateRazorView(string file )
        {
            var cshtml = File.ReadAllText(file);

            // rename /Content/ to /wwwroot/ anywhere
            cshtml = cshtml.Replace("/Content/", "/wwwroot/").Replace("/content/","/wwwroot/");
            cshtml = cshtml.Replace("/Scripts/", "/wwwroot/js/").Replace("/scripts/", "/wwwroot/js/");
            return cshtml;
        }

    }
}