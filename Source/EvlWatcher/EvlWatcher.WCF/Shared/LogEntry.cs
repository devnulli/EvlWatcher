using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvlWatcher.Logging;

namespace EvlWatcher.Logging
{
    public enum SeverityLevel
    {
        Off = 6,
        Critical = 5,
        Error = 4,
        Warning = 3,
        Info = 2,
        Verbose = 1,
        Debug = 0
    };

    public class LogEntry
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public SeverityLevel Severity { get; set; }
    }
}
