using System;

namespace IO.Ably.Tests.Shared.Helpers
{
    /// <summary>An utility class for logging various messages.</summary>
    public static class DefaultLogger
    {
        private static readonly object SyncLock = new object();
        private static ILogger _loggerInstance;

        internal static ILogger LoggerInstance
        {
            get
            {
                if (_loggerInstance == null)
                {
                    lock (SyncLock)
                    {
                        _loggerInstance = InternalLogger.Create();
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

        /// <summary>
        /// The current LoggerSink. When the library is initialised with a LoggerSink
        /// this property gets set.
        /// </summary>
        public static ILoggerSink LoggerSink
        {
            get => LoggerInstance.LoggerSink;
            set => LoggerInstance.LoggerSink = value;
        }

        /// <summary>
        /// IsDebug.
        /// </summary>
        public static bool IsDebug => LoggerInstance.LogLevel == LogLevel.Debug;

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

        /// <summary>Log a warning message.</summary>
        internal static void Warning(string message, params object[] args)
        {
            LoggerInstance.Warning(message, args);
        }

        /// <summary>Log a debug message.</summary>
        internal static void Debug(string message, params object[] args)
        {
            LoggerInstance.Debug(message, args);
        }
    }
}
