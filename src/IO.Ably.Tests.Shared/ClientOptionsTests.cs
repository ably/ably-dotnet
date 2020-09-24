using System;
using System.Collections.Generic;
using System.Text;
using IO.Ably.AcceptanceTests;
using Microsoft.Build.Logging;
using Xunit;

namespace IO.Ably.Tests.Shared
{
    public class ClientOptionsTests
    {
        [Fact]
        [Obsolete]
        public void Options_WithLogHander_ReturnsLogHandlerWithSameLoggerSink()
        {
            var testLoggerSink = new TestLoggerSink();
            var options = new ClientOptions()
            {
                LogHander = testLoggerSink
            };

            Assert.Same(options.LogHander, testLoggerSink);
            Assert.Same(options.LogHandler, testLoggerSink);

            var debugLoggerSink = new DefaultLoggerSink();
            options.LogHander = debugLoggerSink;

            Assert.Same(options.LogHander, debugLoggerSink);
            Assert.Same(options.LogHandler, debugLoggerSink);
        }

        [Fact]
        [Obsolete]
        public void Options_WithLogHandler_ReturnsLogHanderWithSameLoggerSink()
        {
            var testLoggerSink = new TestLoggerSink();
            var options = new ClientOptions()
            {
                LogHandler = testLoggerSink
            };

            Assert.Same(options.LogHandler, testLoggerSink);
            Assert.Same(options.LogHander, testLoggerSink);

            var debugLoggerSink = new DefaultLoggerSink();
            options.LogHandler = debugLoggerSink;

            Assert.Same(options.LogHandler, debugLoggerSink);
            Assert.Same(options.LogHander, debugLoggerSink);
        }
    }
}
