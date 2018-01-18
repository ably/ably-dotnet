﻿using System;
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
            var logger = new DefaultLogger.InternalLogger();

            using (logger.SetTempDestination(null))
            {
                sink.LastLevel.ShouldBeEquivalentTo(null);
                sink.LastMessage.ShouldBeEquivalentTo(null);

                logger.LogLevel.ShouldBeEquivalentTo(Defaults.DefaultLogLevel);
                logger.LogLevel = LogLevel.Debug;

                // null destination shouldn't throw
                logger.LoggerSink = null;
                logger.Debug("msg");

                logger.LoggerSink = sink;

                // Basic messages
                logger.Error("Test Error Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Error);
                sink.LastMessage.Should().EndWith("Test Error Message");

                logger.Debug("Test Info Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Debug);
                sink.LastMessage.Should().EndWith("Test Info Message");

                logger.Debug("Test Debug Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Debug);
                sink.LastMessage.Should().EndWith("Test Debug Message");

                // Verify the log level works
                logger.LogLevel = LogLevel.Warning;
                logger.Error("Test Error Message");
                logger.Debug("Test Info Message");
                logger.Debug("Test Debug Message");
                sink.LastLevel.ShouldBeEquivalentTo(LogLevel.Error);
                sink.LastMessage.Should().EndWith("Test Error Message");

                // Revert the level
                logger.LogLevel = Defaults.DefaultLogLevel;
            }

            // test that the Logger gets instanced
            Assert.NotNull(DefaultLogger.LoggerInstance);
        }

        [Fact]
        public void ClientOptionsWithNoLoggerSpecified_ShouldUseTheDefaultLogger()
        {
            var opts = new ClientOptions();
            Assert.Same(opts.Logger, DefaultLogger.LoggerInstance);
        }

        [Fact]
        public void LoggerInstances_ShouldNotInteract()
        {
            var logger1 = new DefaultLogger.InternalLogger();
            var logger2 = new DefaultLogger.InternalLogger();

            logger1.LogLevel.ShouldBeEquivalentTo(logger2.LogLevel);
            logger1.LogLevel = LogLevel.Debug;
            logger2.LogLevel = LogLevel.Error;

            logger1.LogLevel.ShouldBeEquivalentTo(LogLevel.Debug);
            logger2.LogLevel.ShouldBeEquivalentTo(LogLevel.Error);
            logger1.LogLevel.Should().NotBe(logger2.LogLevel);
        }


        public void Dispose()
        {
            DefaultLogger.LoggerSink = new DefaultLoggerSink();
        }
    }
}