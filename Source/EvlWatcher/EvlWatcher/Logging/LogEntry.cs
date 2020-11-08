using System;


namespace EvlWatcher.Logging
{ 
    public class LogEntry
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public SeverityLevel Severity { get; set; }
    }
}

