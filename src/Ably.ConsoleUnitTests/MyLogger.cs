using System;
using System.Collections.Generic;

namespace IO.Ably.ConsoleTest
{
    /// <summary>ILoggerSink implementation that outputs colored messages to console.</summary>
    class MyLogger : ILoggerSink
    {
        static readonly Dictionary<LogLevel, ConsoleColor> s_colors = new Dictionary<LogLevel, ConsoleColor>()
        {
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Warning, ConsoleColor.Yellow},
            { LogLevel.Info, ConsoleColor.White },
            { LogLevel.Debug, ConsoleColor.Cyan },
        };

        void ILoggerSink.LogEvent(LogLevel level, string message)
        {
            ConsoleEx.WriteLine(s_colors[level], "    " + message);
        }
    }
}