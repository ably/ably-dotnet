using System;
using IO.Ably.AcceptanceTests;
using Xunit;

namespace IO.Ably.Tests.Shared
{
    public class DefaultTests
    {
        [Fact]
        public void Defaults_WithEnvironment_ReturnsEnvironmentFallbackHosts()
        {
            var expectedFallBackHosts = new[]
            {
                "sandbox-a-fallback.ably-realtime.com",
                "sandbox-b-fallback.ably-realtime.com",
                "sandbox-c-fallback.ably-realtime.com",
                "sandbox-d-fallback.ably-realtime.com",
                "sandbox-e-fallback.ably-realtime.com"
            };
            var fallbackHosts = Defaults.GetEnvironmentFallbackHosts("sandbox");
            Assert.Equal(expectedFallBackHosts, fallbackHosts);
        }
    }
}
