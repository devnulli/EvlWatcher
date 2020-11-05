﻿using EvlWatcher.Logging;
using System;

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
}
public interface ILogger
{
    SeverityLevel LogLevel { get; set; }
    SeverityLevel ConsoleLevel { get; set; }

    void Dump(string message, SeverityLevel severity);
    void Dump(Exception e, SeverityLevel severity = SeverityLevel.Error);

}
