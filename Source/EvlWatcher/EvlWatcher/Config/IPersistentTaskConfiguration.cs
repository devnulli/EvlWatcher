using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvlWatcher.Config
{
    public interface IPersistentTaskConfiguration
    {
        string TaskName { get; }
        string Description { get; set; }
        int LockTime { get; set; }
        bool OnlyNewEvents { get; set; }
        int EventAge { get; set; }
        int TriggerCount { get; set; }
        int PermaBanCount { get; set; }
        string EventPath { get; set; }
        IQueryable<string> RegexBoosters { get; }
        bool AddRegexBooster(string regexBooster);
        bool RemoveRegexBooster(string regexBooster);
        string Regex { get; set; }
    }
}
