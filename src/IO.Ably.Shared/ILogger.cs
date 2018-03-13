using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably
{
    internal interface ILogger
    {
        LogLevel LogLevel { get; set; }

        bool IsDebug { get; }

        ILoggerSink LoggerSink { get; set; }

        void Error(string message, Exception ex);

        void Error(string message, params object[] args);

        void Warning(string message, params object[] args);

        void Debug(string message, params object[] args);
    }
}
