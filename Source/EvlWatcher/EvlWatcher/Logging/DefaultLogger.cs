using EvlWatcher.Converter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EvlWatcher.Logging
{
    internal class DefaultLogger : ILogger
    {
        private readonly object _syncObject = new object();
        public SeverityLevel LogLevel { get; set; } = SeverityLevel.Warning;

        private int ConsoleHistoryMaxCount { get; set; } = 1000;
        private IList<LogEntry> ConsoleHistory { get; set; } = new List<LogEntry>();

        private void ManageConsoleHistory(string message, SeverityLevel severity, DateTime date)
        {
            lock (_syncObject)
            {
                if (ConsoleHistory.Count >= ConsoleHistoryMaxCount)
                {
                    ConsoleHistory.RemoveAt(0);
                }
                ConsoleHistory.Add(new LogEntry() { Message = message, Severity = severity, Date = date });
            }
        }

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

            var date = DateTime.Now;

            if (Environment.UserInteractive)
                Console.WriteLine($"{date.Hour}:{date.Minute}:{date.Second},{date.Millisecond} {message}");

            ManageConsoleHistory(message, severity, date);

        }

        public void Dump(Exception e, SeverityLevel level = SeverityLevel.Error)
        {
            Dump(e.Message, level);
        }
        /// <summary>
        /// Returns Console History, max count is defined in ConsoleHistoryMaxCount
        /// </summary>
        /// <returns></returns>
        public IList<LogEntry> GetConsoleHistory()
        {
            lock (_syncObject)
            {
                return ConsoleHistory.ToList();
            }
        }
        /// <summary>
        /// Default ConsoleHistoryMaxCount is 1000
        /// </summary>
        /// <param name="count"></param>
        public void SetConsoleHistoryMaxCount(int count)
        {
            ConsoleHistoryMaxCount = count;
        }
        /// <summary>
        /// Returns ConsoleHistoryMaxCount, default is 1000
        /// </summary>
        /// <returns>int</returns>
        public int GetConsoleHistoryMaxCount()
        {
            return ConsoleHistoryMaxCount;
        }
    }
}
