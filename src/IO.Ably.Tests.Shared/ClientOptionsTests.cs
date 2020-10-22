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

        [Fact]
        public void DefaultOptions()
        {
            var options = new ClientOptions();
            Assert.Equal("realtime.ably.io", options.RealtimeHost);
            Assert.Equal("rest.ably.io", options.RestHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.FallbackHosts, options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        public void Options_WithProductionEnvironment()
        {
            var options = new ClientOptions()
            {
                Environment = "production"
            };
            Assert.Equal("realtime.ably.io", options.RealtimeHost);
            Assert.Equal("rest.ably.io", options.RestHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.FallbackHosts, options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        public void Options_WithCustomEnvironment()
        {
            var options = new ClientOptions()
            {
                Environment = "sandbox"
            };
            Assert.Equal("sandbox-realtime.ably.io", options.RealtimeHost);
            Assert.Equal("sandbox-rest.ably.io", options.RestHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.GetEnvironmentFallbackHosts("sandbox"), options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        [Obsolete]
        public void Options_WithCustomEnvironment_And_DefaultFallbacks()
        {
            var options = new ClientOptions()
            {
                Environment = "sandbox",
                FallbackHostsUseDefault = true
            };

            Assert.Equal("sandbox-realtime.ably.io", options.RealtimeHost);
            Assert.Equal("sandbox-rest.ably.io", options.RestHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.FallbackHosts, options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        public void Options_WithCustomEnvironment_And_NonDefaultPorts()
        {
            var options = new ClientOptions()
            {
                Environment = "local",
                Port = 8080,
                TlsPort = 8081
            };

            Assert.Equal("local-realtime.ably.io", options.RealtimeHost);
            Assert.Equal("local-rest.ably.io", options.RestHost);
            Assert.Equal(8080, options.Port);
            Assert.Equal(8081, options.TlsPort);
            Assert.Empty(options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        public void Options_WithCustomHost()
        {
            var options = new ClientOptions()
            {
                RestHost = "test.org"
            };

            Assert.Equal("test.org", options.RestHost);
            Assert.Equal("test.org", options.RealtimeHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Empty(options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        public void Options_WithCustomRestHost_And_RealtimeHost()
        {
            var options = new ClientOptions()
            {
                RestHost = "test.org",
                RealtimeHost = "ws.test.org"
            };

            Assert.Equal("test.org", options.RestHost);
            Assert.Equal("ws.test.org", options.RealtimeHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Empty(options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        [Obsolete]
        public void Options_WithCustomRestHost_And_RealtimeHost_And_DefaultFallbacks()
        {
            var options = new ClientOptions()
            {
                RestHost = "test.org",
                RealtimeHost = "ws.test.org",
                FallbackHostsUseDefault = true
            };

            Assert.Equal("test.org", options.RestHost);
            Assert.Equal("ws.test.org", options.RealtimeHost);
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.FallbackHosts, options.FallbackHosts);
            Assert.True(options.Tls);
        }

        [Fact]
        [Obsolete]
        public void Options_With_FallbackHosts_And_FallbackHostsUseDefault()
        {
            var options = new ClientOptions()
            {
                FallbackHosts = new[] { "a.example.com", "b.example.com" },
                FallbackHostsUseDefault = true
            };

            var ex = Assert.Throws<AblyException>(() => { var unused = options.FallbackHosts; });
            Assert.Equal("fallbackHosts and fallbackHostsUseDefault cannot both be set", ex.ErrorInfo.Message);
        }

        [Fact]
        [Obsolete]
        public void Options_With_And_FallbackHostsUseDefault_And_Port_Or_TlsPort()
        {
            var options = new ClientOptions()
            {
                FallbackHostsUseDefault = true,
                Port = 8080
            };

            var ex = Assert.Throws<AblyException>(() =>
            {
                var unused = options.FallbackHosts;
            });
            Assert.Equal("fallbackHostsUseDefault cannot be set when port or tlsPort are set", ex.ErrorInfo.Message);

            options = new ClientOptions()
            {
                FallbackHostsUseDefault = true,
                TlsPort = 8081
            };

            ex = Assert.Throws<AblyException>(() =>
            {
                var unused = options.FallbackHosts;
            });
            Assert.Equal("fallbackHostsUseDefault cannot be set when port or tlsPort are set", ex.ErrorInfo.Message);
        }
    }
}
