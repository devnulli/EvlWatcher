﻿using EvlWatcher.Logging;
using System.Diagnostics;

namespace EvlWatcher.Converter
{
    class SeverityToEventLogEntryType
    {
        public static EventLogEntryType Convert(SeverityLevel level)
        {
            switch(level)
            {
                case SeverityLevel.Critical:
                case SeverityLevel.Error:
                    return EventLogEntryType.Error;

                case SeverityLevel.Warning:
                    return EventLogEntryType.Warning;

                case SeverityLevel.Verbose:
                case SeverityLevel.Info:
                case SeverityLevel.Debug:
                    return EventLogEntryType.Information;

                default: return EventLogEntryType.Error;
            }
        }
    }
}
