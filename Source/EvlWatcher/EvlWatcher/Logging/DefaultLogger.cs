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
        private SeverityLevel _loglevel = SeverityLevel.Error;
        private SeverityLevel _outputlevel = SeverityLevel.Verbose;

        public void Dump(string message, SeverityLevel severity)
        {
            string source = "EvlWatcher";
            string log = "Application";

            //you must run this as admin for the first time - so that the eventlog source can be created
            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, log);

            if (severity >= _loglevel)
            {
                EventLog.WriteEntry(source, message, SeverityToEventLogEntryType.Convert(severity));
            }
            if (severity >= _outputlevel)
            {
                Console.WriteLine($"{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second},{DateTime.Now.Millisecond} {message}");
            }
        }

        public void Dump(Exception e, SeverityLevel level = SeverityLevel.Error)
        {
            Dump(e.Message, level);
        }

        public void SetLogLevel(SeverityLevel newLogLevel)
        {
            _loglevel = newLogLevel;
        }

        public void SetOutputLevel(SeverityLevel newOutputLevel)
        {
            _outputlevel = newOutputLevel;
        }
    }
}
