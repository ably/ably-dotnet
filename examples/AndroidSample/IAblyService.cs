using System;

namespace AndroidSample
{
    public class LogMessage
    {
        public string Level { get; set; }
        public string Message { get; set; }

        public LogMessage(string message, string level)
        {
            Message = message;
            Level = level;
        }

    }
}