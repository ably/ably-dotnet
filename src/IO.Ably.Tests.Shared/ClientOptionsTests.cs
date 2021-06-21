using System;
using IO.Ably.AcceptanceTests;
using Xunit;

namespace IO.Ably.Tests.Shared
{
    public class ClientOptionsTests
    {
        [Fact]
        [Trait("spec", "RSC15e")]
        [Trait("spec", "RSC15g3")]
        public void DefaultOptions()
        {
            var options = new ClientOptions();
            Assert.Equal("realtime.ably.io", options.FullRealtimeHost());
            Assert.Equal("rest.ably.io", options.FullRestHost());
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.FallbackHosts, options.GetFallbackHosts());
            Assert.True(options.Tls);
        }

        [Fact]
        [Trait("spec", "RSC15h")]
        public void Options_WithProductionEnvironment()
        {
            var options = new ClientOptions()
            {
                Environment = "production"
            };
            Assert.Equal("realtime.ably.io", options.FullRealtimeHost());
            Assert.Equal("rest.ably.io", options.FullRestHost());
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.FallbackHosts, options.GetFallbackHosts());
            Assert.True(options.Tls);
        }

        [Fact]
        [Trait("spec", "RSC15g2")]
        [Trait("spec", "RTC1e")]
        public void Options_WithCustomEnvironment()
        {
            var options = new ClientOptions()
            {
                Environment = "sandbox"
            };
            Assert.Equal("sandbox-realtime.ably.io", options.FullRealtimeHost());
            Assert.Equal("sandbox-rest.ably.io", options.FullRestHost());
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Equal(Defaults.GetEnvironmentFallbackHosts("sandbox"), options.GetFallbackHosts());
            Assert.True(options.Tls);
        }

        [Fact]
        [Trait("spec", "RSC11b")]
        [Trait("spec", "RTN17b")]
        [Trait("spec", "RTC1e")]
        public void Options_WithCustomEnvironment_And_NonDefaultPorts()
        {
            var options = new ClientOptions()
            {
                Environment = "local",
                Port = 8080,
                TlsPort = 8081
            };

            Assert.Equal("local-realtime.ably.io", options.FullRealtimeHost());
            Assert.Equal("local-rest.ably.io", options.FullRestHost());
            Assert.Equal(8080, options.Port);
            Assert.Equal(8081, options.TlsPort);
            Assert.Empty(options.GetFallbackHosts());
            Assert.True(options.Tls);
        }

        [Fact]
        [Trait("spec", "RSC11")]
        public void Options_WithCustomRestHost()
        {
            var options = new ClientOptions()
            {
                RestHost = "test.org"
            };

            Assert.Equal("test.org", options.FullRestHost());
            Assert.Equal("test.org", options.FullRealtimeHost());
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Empty(options.GetFallbackHosts());
            Assert.True(options.Tls);
        }

        [Fact]
        [Trait("spec", "RSC11")]
        public void Options_WithCustomRestHost_And_RealtimeHost()
        {
            var options = new ClientOptions()
            {
                RestHost = "test.org",
                RealtimeHost = "ws.test.org"
            };

            Assert.Equal("test.org", options.FullRestHost());
            Assert.Equal("ws.test.org", options.FullRealtimeHost());
            Assert.Equal(80, options.Port);
            Assert.Equal(443, options.TlsPort);
            Assert.Empty(options.GetFallbackHosts());
            Assert.True(options.Tls);
        }
    }
}
