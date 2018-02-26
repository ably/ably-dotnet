using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using IO.Ably;

namespace IO.Ably
{
    /// <summary>Level of a log message.</summary>
    public enum LogLevel : byte
    {
        Debug = 0,
        Warning,
        Error,
        None = 99
    }

    /// <summary>An interface that actually logs that messages somewhere.</summary>
    public interface ILoggerSink
    {
        void LogEvent(LogLevel level, string message);
    }

    /// <inheritdoc />
    /// <summary>The default logger implementation, that writes to debug output.</summary>
    internal class DefaultLoggerSink : ILoggerSink
    {
        private readonly object _syncRoot = new object();
        public void LogEvent(LogLevel level, string message)
        {
            lock (_syncRoot)
            {
                Debug.WriteLine("Ably: [{0}] {1}", level, message);
            }
        }
    }

    /// <summary>An utility class for logging various messages.</summary>
    public static class DefaultLogger
    {
        private static readonly object SyncLock = new Object();
        private static InternalLogger _loggerInstance;
        internal static InternalLogger LoggerInstance
        {
            get
            {
                if (_loggerInstance == null)
                {
                    lock (SyncLock)
                    {
                        _loggerInstance = new InternalLogger();
                    }
                }

                return _loggerInstance;
            }

            set => _loggerInstance = value;
        }

        /// <summary>Maximum level to log.</summary>
        /// <remarks>E.g. set to LogLevel.Warning to have only errors and warnings in the log.</remarks>
        public static LogLevel LogLevel
        {
            get => LoggerInstance.LogLevel;
            set => LoggerInstance.LogLevel = value;
        }

        public static ILoggerSink LoggerSink
        {
            get => LoggerInstance.LoggerSink;
            set => LoggerInstance.LoggerSink = value;
        }

        public static bool IsDebug => LoggerInstance.LogLevel == LogLevel.Debug;


        internal static IDisposable SetTempDestination(ILoggerSink i)
        {
            ILoggerSink o = LoggerInstance.LoggerSink;
            LoggerInstance.LoggerSink = i;
            return new ActionOnDispose(() => LoggerInstance.LoggerSink = o);
        }

        /// <summary>Log an error message.</summary>
        internal static void Error(string message, Exception ex)
        {
            LoggerInstance.Error(message, ex);
        }

        /// <summary>Log an error message.</summary>
        internal static void Error(string message, params object[] args)
        {
            LoggerInstance.Error(message, args);
        }

        /// <summary>Log a warning message</summary>
        internal static void Warning(string message, params object[] args)
        {
            LoggerInstance.Warning(message, args);
        }

        /// <summary>Log a debug message.</summary>
        internal static void Debug(string message, params object[] args)
        {
            LoggerInstance.Debug(message, args);
        }


        internal class InternalLogger : ILogger
        {
            /// <summary>Maximum level to log.</summary>
            /// <remarks>E.g. set to LogLevel.Warning to have only errors and warnings in the log.</remarks>
            public LogLevel LogLevel { get; set; }

            public ILoggerSink LoggerSink { get; set; }
            public bool IsDebug => LogLevel == LogLevel.Debug;

            internal Func<DateTimeOffset> Now { get; set; }

            public InternalLogger()
                : this(Defaults.DefaultLogLevel, new DefaultLoggerSink()) { }
            public InternalLogger(ILoggerSink loggerSink)
                : this(Defaults.DefaultLogLevel, loggerSink) { }
            public InternalLogger(LogLevel logLevel, ILoggerSink loggerSink)
                : this(logLevel, loggerSink, null ) { }
            public InternalLogger(LogLevel logLevel, ILoggerSink loggerSink, Func<DateTimeOffset> nowProvider)
            {
                LogLevel = logLevel;
                LoggerSink = loggerSink;
                Now = nowProvider ?? Defaults.NowFunc();
            }

            public IDisposable SetTempDestination(ILoggerSink i)
            {
                ILoggerSink o = LoggerSink;
                LoggerSink = i;
                return new ActionOnDispose(() => LoggerSink = o);
            }

            public void Message(LogLevel level, string message, params object[] args)
            {
                var timeStamp = GetLogMessagePreifx();
                ILoggerSink loggerSink = LoggerSink;
                if (LogLevel == LogLevel.None || level < LogLevel || loggerSink == null)
                {
                    return;
                }

                if (args == null || args.Length == 0)
                {
                    loggerSink.LogEvent(level, timeStamp + " " + message);
                }
                else
                {
                    loggerSink.LogEvent(level, timeStamp + " " + string.Format(message, args));
                }
            }

            public string GetLogMessagePreifx()
            {
                var timeStamp = Now().ToString("hh:mm:ss.fff");
                return $"{timeStamp}";
            }

            /// <summary>Log an error message.</summary>
            public void Error(string message, Exception ex)
            {
                Message(LogLevel.Error, "{0} {1}", message, GetExceptionDetails(ex));
            }

            /// <summary>Log an error message.</summary>
            public void Error(string message, params object[] args)
            {
                Message(LogLevel.Error, message, args);
            }

            /// <summary>Log a warning message</summary>
            public void Warning(string message, params object[] args)
            {
                Message(LogLevel.Warning, message, args);
            }

            /// <summary>Log a debug message.</summary>
            public void Debug(string message, params object[] args)
            {
                Message(LogLevel.Debug, message, args);
            }

            /// <summary>Produce long multiline string with the details about the exception, including inner exceptions, if any.</summary>
            private string GetExceptionDetails(Exception ex)
            {
                var message = new StringBuilder();
                var ablyException = ex as AblyException;
                if (ablyException != null)
                {
                    message.AppendLine("Error code: " + ablyException.ErrorInfo.Code);
                    message.AppendLine("Status code: " + ablyException.ErrorInfo.StatusCode);
                    message.AppendLine("Reason: " + ablyException.ErrorInfo.Message);
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
}