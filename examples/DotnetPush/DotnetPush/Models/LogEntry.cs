using IO.Ably;

namespace DotnetPush.Models
{
    /// <summary>
    /// Class used to keep Ably log entries.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="level">Log level.</param>
        /// <param name="message">Log message.</param>
        public LogEntry(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        /// <summary>
        /// Log level property.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Log message.
        /// </summary>
        public string Message { get; set; }
    }
}
