using System;
using FluentAssertions;
using IO.Ably.Transport;
using Xunit;

namespace IO.Ably.AcceptanceTests
{
    public class TestLoggerSink : ILoggerSink
    {
        void ILoggerSink.LogEvent(LogLevel level, string message)
        {
            LastLevel = level;
            LastMessage = message;
        }

        public LogLevel? LastLevel { get; set; }
        public string LastMessage { get; set; }
    }

    public class LoggerTests : IDisposable
    {
        [Fact]
        public void TestLogger()
        {
            var sink = new TestLoggerSink();

            using (Logger.SetTempDestination(null))
            {
                sink.LastLevel.ShouldBeEquivalentTo(null);
                sink.LastMessage.ShouldBeEquivalentTo(null);

                Logger.LogLevel.ShouldBeEquivalentTo(Defaults.DefaultLogLevel);
                Logger.LogLevel = LogLevel.Debug;

                // null destination shouldn't throw
                Logger.LoggerSink = null;
                Logger.Info("msg");

                Logger.LoggerSink = sink;

                // Basic messages
                Logger.Error("Test Error Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Error);
                sink.LastMessage.ShouldBeEquivalentTo("Test Error Message");

                Logger.Info("Test Info Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Info);
                sink.LastMessage.ShouldBeEquivalentTo("Test Info Message");

                Logger.Debug("Test Debug Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Debug);
                sink.LastMessage.ShouldBeEquivalentTo("Test Debug Message");

                // Verify the log level works
                Logger.LogLevel = LogLevel.Warning;
                Logger.Error("Test Error Message");
                Logger.Info("Test Info Message");
                Logger.Debug("Test Debug Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Error);
                sink.LastMessage.ShouldBeEquivalentTo("Test Error Message");

                // Revert the level
                Logger.LogLevel = Defaults.DefaultLogLevel;
            }
        }


        public void Dispose()
        {
            Logger.LoggerSink = new DefaultLoggerSink();
        }
    }
}