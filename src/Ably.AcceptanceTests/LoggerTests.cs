using NUnit.Framework;
using System.Diagnostics;
using FluentAssertions;
using IO.Ably.Transport;

namespace IO.Ably.AcceptanceTests
{
    [TestFixture]
    class LoggerTests
    {
        class TestLoggerSink : ILoggerSink
        {
            void ILoggerSink.LogEvent(LogLevel level, string message)
            {
                lastLevel = level;
                lastMessage = message;
            }

            public LogLevel? lastLevel;
            public string lastMessage;
        }

        TestLoggerSink sink = new TestLoggerSink();

        public LoggerTests()
        {

        }


        [Test]
        public void TestLogger()
        {
            using (var d = Logger.SetTempDestination(null))
            {
                sink.lastLevel.ShouldBeEquivalentTo(null);
                sink.lastMessage.ShouldBeEquivalentTo(null);

                Logger.LogLevel.ShouldBeEquivalentTo(Defaults.DefaultLogLevel);
                Logger.LogLevel = LogLevel.Debug;

                // null destination shouldn't throw
                Logger.LoggerSink = null;
                Logger.Info("msg");

                Logger.LoggerSink = sink;

                // Basic messages
                Logger.Error("Test Error Message");
                sink.lastLevel.ShouldBeEquivalentTo(LogLevel.Error);
                sink.lastMessage.ShouldBeEquivalentTo("Test Error Message");

                Logger.Info("Test Info Message");
                sink.lastLevel.ShouldBeEquivalentTo(LogLevel.Info);
                sink.lastMessage.ShouldBeEquivalentTo("Test Info Message");

                Logger.Debug("Test Debug Message");
                sink.lastLevel.ShouldBeEquivalentTo(LogLevel.Debug);
                sink.lastMessage.ShouldBeEquivalentTo("Test Debug Message");

                // Verify the log level works
                Logger.LogLevel = LogLevel.Warning;
                Logger.Error("Test Error Message");
                Logger.Info("Test Info Message");
                Logger.Debug("Test Debug Message");
                sink.lastLevel.ShouldBeEquivalentTo(LogLevel.Error);
                sink.lastMessage.ShouldBeEquivalentTo("Test Error Message");

                // Revert the level
                Logger.LogLevel = Defaults.DefaultLogLevel;
            }
        }
    }
}