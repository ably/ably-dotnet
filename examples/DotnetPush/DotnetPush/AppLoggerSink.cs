using System.Collections.Generic;
using DotnetPush.Models;
using IO.Ably;

namespace DotnetPush
{
    /// <inheritdoc />
    public class AppLoggerSink : ILoggerSink
    {
        /// <summary>
        /// Exposes a List of LogEntries.
        /// </summary>
        public List<LogEntry> Messages { get; set; } = new List<LogEntry>();

        /// <inheritdoc/>
        public void LogEvent(LogLevel level, string message)
        {
            Messages.Add(new LogEntry(level, message));
        }
    }
}
