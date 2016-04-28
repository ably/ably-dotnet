using System;
using System.Collections.Generic;

namespace IO.Ably.ConsoleTest
{
    /// <summary>ILoggerSink implementation that outputs colored messages to console.</summary>
    class MyLogger : ILoggerSink
    {
        static readonly Dictionary<LogLevel, ConsoleColor> s_colors = new Dictionary<LogLevel, ConsoleColor>()
        {
            { LogLevel.Error, ConsoleColor.DarkRed },
            { LogLevel.Warning, ConsoleColor.DarkYellow },
            { LogLevel.Info, ConsoleColor.DarkGreen },
            { LogLevel.Debug, ConsoleColor.DarkBlue },
        };

        void ILoggerSink.LogEvent( LogLevel level, string message )
        {
            ConsoleEx.writeLine( s_colors[ level ], "    " + message );
        }
    }
}