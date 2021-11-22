using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably;

namespace DotnetPush.Models
{
    /// <summary>
    /// Used to keep data from remote messages.
    /// </summary>
    public class PushNotification
    {
        /// <summary>
        /// Title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Flattened Data dictionary.
        /// </summary>
        public string DataText => Data == null ? "No data" : string.Join(" | ", Data.Select(kv => $"{kv.Key}-{kv.Value}"));

        /// <summary>
        /// Additional data.
        /// </summary>
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// When the phone received the message.
        /// </summary>
        public DateTimeOffset Received { get; set; }
    }

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
