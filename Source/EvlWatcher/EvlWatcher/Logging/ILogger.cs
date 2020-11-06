using EvlWatcher.Logging;
using System;
using System.Collections.Generic;



public interface ILogger
{
    SeverityLevel LogLevel { get; set; }
    SeverityLevel ConsoleLevel { get; set; }

    void Dump(string message, SeverityLevel severity);
    void Dump(Exception e, SeverityLevel severity = SeverityLevel.Error);
    int GetConsoleHistoryMaxCount();
    void SetConsoleHistoryMaxCount(int count);
    IList<LogEntry> GetConsoleHistory();
}

