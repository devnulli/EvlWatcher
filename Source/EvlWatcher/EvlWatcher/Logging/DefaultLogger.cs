using EvlWatcher.Converter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvlWatcher.Logging
{
    internal class DefaultLogger : ILogger
    {
        public SeverityLevel LogLevel { get; set; } = SeverityLevel.Warning;

        public SeverityLevel ConsoleLevel { get; set; } = SeverityLevel.Info;

        public void Dump(string message, SeverityLevel severity)
        {
            string source = "EvlWatcher";
            string log = "Application";

            if (severity >= LogLevel)
            {
                //you must run this as admin for the first time - so that the eventlog source can be created
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource(source, log);

                EventLog.WriteEntry(source, message, SeverityToEventLogEntryType.Convert(severity));
            }
            if (severity >= ConsoleLevel)
            {
                Console.WriteLine($"{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second},{DateTime.Now.Millisecond} {message}");
            }
        }

        public void Dump(Exception e, SeverityLevel level = SeverityLevel.Error)
        {
            Dump(e.Message, level);
        }
    }
}
