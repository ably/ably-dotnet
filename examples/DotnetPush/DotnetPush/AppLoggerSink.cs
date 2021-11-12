using System.Collections.Generic;
using System.Linq;
using DotnetPush.Models;
using IO.Ably;

namespace DotnetPush
{
    /// <inheritdoc />
    public class AppLoggerSink : ILoggerSink
    {
        private object _lock = new object();

        /// <summary>
        /// Exposes a List of LogEntries.
        /// </summary>
        private List<LogEntry> Messages { get; set; } = new List<LogEntry>();

        /// <summary>
        /// Get messages.
        /// </summary>
        /// <returns>Returns logged messages.</returns>
        public IEnumerable<LogEntry> GetMessages()
        {
            lock (_lock)
            {
                return Messages.ToList();
            }
        }

        /// <inheritdoc/>
        public void LogEvent(LogLevel level, string message)
        {
            lock (_lock)
            {
                Messages.Add(new LogEntry(level, message));
            }
        }
    }
}
