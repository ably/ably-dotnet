using IO.Ably;

namespace DotnetPush.Models
{
    public class LogEntry
    {
        public LogEntry(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public LogLevel Level { get; set; }
        public string Message { get; set; }
    }


}