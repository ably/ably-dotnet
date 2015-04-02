using System;

namespace Ably
{
    /// <summary>
    /// The NullLogger can be used so all log messages are silenced. 
    /// <code>Config.AblyLogger = NullLogger.Instance</code>
    /// </summary>
    public class NullLogger : ILogger
    {
        public readonly static ILogger Instance = new NullLogger();

        private NullLogger()
        {
            
        }

        public void Error(string message, Exception ex)
        {
            
        }

        public void Error(string message, params object[] args)
        {
        }

        public void Info(string message, params object[] args)
        {
        }

        public void Debug(string message)
        {
        }

        public void Verbose(string message, params object[] args)
        {
        }
    }
}