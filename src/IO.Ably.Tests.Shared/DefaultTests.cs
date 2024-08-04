using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests.Shared
{
    public class DefaultTests
    {
        [Fact]
        [Trait("spec", "RSC15h")]
        public void Defaults_ReturnsFallbackHosts()
        {
            var expectedFallBackHosts = new[]
            {
                "a.ably-realtime.com",
                "b.ably-realtime.com",
                "c.ably-realtime.com",
                "d.ably-realtime.com",
                "e.ably-realtime.com",
            };
            var fallbackHosts = Defaults.FallbackHosts;
            Assert.Equal(expectedFallBackHosts, fallbackHosts);
        }

        [Fact]
        [Trait("spec", "RSC15i")]
        public void Defaults_WithEnvironment_ReturnsEnvironmentFallbackHosts()
        {
            var expectedFallBackHosts = new[]
            {
                "lmars-dev-a-fallback.ably-realtime.com",
                "lmars-dev-b-fallback.ably-realtime.com",
                "lmars-dev-c-fallback.ably-realtime.com",
                "lmars-dev-d-fallback.ably-realtime.com",
                "lmars-dev-e-fallback.ably-realtime.com",
            };
            var fallbackHosts = Defaults.GetEnvironmentFallbackHosts("lmars-dev");
            Assert.Equal(expectedFallBackHosts, fallbackHosts);
        }

        [Fact]
        public void Defaults_ProtocolIsJson()
        {
            Defaults.Protocol.Should().Be(Protocol.Json);
        }
    }
}
