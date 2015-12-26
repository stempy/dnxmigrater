using System.Collections.Generic;

namespace DnxMigrater.Models.Source
{
    public class CsConfigSection : Dictionary<string, string>
    {
        public CsConfigSection(IDictionary<string, string> items)
        {
            this.Clear();
            foreach (var item in items)
            {
                Add(item.Key,item.Value);
            }
        }
    }
}