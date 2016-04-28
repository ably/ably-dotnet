using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text;
using IO.Ably.Transport;

namespace IO.Ably
{
    /// <summary>Level of a log message.</summary>
    public enum LogLevel : byte
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Debug = 3,
    }

    /// <summary>An interface that actually logs that messages somewhere.</summary>
    public interface ILoggerSink
    {
        void LogEvent(LogLevel level, string message);
    }

    /// <summary>The default logger implementation, that writes to debug output.</summary>
    internal class DefaultLoggerSink : ILoggerSink
    {
        readonly object syncRoot = new object();

        public void LogEvent(LogLevel level, string message)
        {
            lock (syncRoot)
            {
                Debug.WriteLine("Ably: [{0}] {1}", level, message);
            }
        }
    }

    /// <summary>An utility class for logging various messages.</summary>
    public static class Logger
    {
        /// <summary>Maximum level to log.</summary>
        /// <remarks>E.g. set to LogLevel.Warning to have only errors and warnings in the log.</remarks>
        public static LogLevel LogLevel { get; set; }

        public static ILoggerSink LoggerSink { get; set; }
        public static bool IsDebug => LogLevel == LogLevel.Debug;

        static Logger()
        {
            LogLevel = Defaults.DefaultLogLevel;
            LoggerSink = new DefaultLoggerSink();
        }

        internal static IDisposable SetTempDestination(ILoggerSink i)
        {
            ILoggerSink o = LoggerSink;
            LoggerSink = i;
            return new ActionOnDispose(() => LoggerSink = o);
        }

        static void Message(LogLevel level, string message, params object[] args)
        {
            ILoggerSink i = LoggerSink;
            if (level > LogLevel || null == i)
                return;
            i.LogEvent(level, string.Format(message, args));
        }

        /// <summary>Log an error message.</summary>
        internal static void Error(string message, Exception ex)
        {
            Message(LogLevel.Error, "{0} {1}", message, GetExceptionDetails(ex));
        }

        /// <summary>Log an error message.</summary>
        internal static void Error(string message, params object[] args)
        {
            Message(LogLevel.Error, message, args);
        }

        /// <summary>Log a warning message</summary>
        internal static void Warning(string message, params object[] args)
        {
            Message(LogLevel.Warning, message, args);
        }

        /// <summary>Log an informational message.</summary>
        internal static void Info(string message, params object[] args)
        {
            Message(LogLevel.Info, message, args);
        }

        /// <summary>Log a debug message.</summary>
        internal static void Debug(string message, params object[] args)
        {
            Message(LogLevel.Debug, message, args);
        }

        /// <summary>Produce long multiline string with the details about the exception, including inner exceptions, if any.</summary>
        static string GetExceptionDetails(Exception ex)
        {
            var message = new StringBuilder();
            var ablyException = ex as AblyException;
            if (ablyException != null)
            {
                message.AppendLine("Error code: " + ablyException.ErrorInfo.code);
                message.AppendLine("Status code: " + ablyException.ErrorInfo.statusCode);
                message.AppendLine("Reason: " + ablyException.ErrorInfo.message);
            }

            message.AppendLine(ex.Message);
            message.AppendLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                message.AppendLine("Inner exception:");
                message.AppendLine(GetExceptionDetails(ex.InnerException));
            }
            return message.ToString();
        }
    }
}