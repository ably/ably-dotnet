using System;

namespace App2
{
    public interface IAblyService : IObservable<LogMessage>, IObservable<string>
    {
        void Connect();
        void SendMessage(string channel, string name, string value);
    }

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