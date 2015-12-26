using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DnxMigrater.Models.Dest
{
    public class GlobalJsonObj
    {
        [JsonProperty("projects")]
        public IEnumerable<string> Sources { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string GetGlobalJsonReferenceForSln()
        {
            var f = File.ReadAllText(@"globaljson_slnreference.txt");
            return f;
        }
    }
}
