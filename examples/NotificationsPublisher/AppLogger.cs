using System;
using System.Collections.Generic;
using IO.Ably;

namespace NotificationsPublisher
{
    public class AppLogger : ILoggerSink
    {
        private int maxMessages = 1000;
        public List<string> Messages = new List<string>();
        private object _lock = new object();

        public void LogEvent(LogLevel level, string message)
        {
            if (message.Contains("HeartbeatMonitor"))
                return;

            lock (_lock)
            {
                var msg = $"{level}: {message}";
                Messages.Add(msg);

                if (Messages.Count > maxMessages)
                {
                    Messages.RemoveAt(0);
                }

                OnMessageAdded(msg);
            }
        }

        public Action<string> OnMessageAdded { get; set; }
    }
}