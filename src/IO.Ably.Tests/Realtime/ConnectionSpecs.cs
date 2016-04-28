using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ConnectionSpecs : AblySpecs
    {
        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        [Trait("spec", "RTN1")]
        public async Task ShouldUseWebSocketTransport()
        {
            var client = GetClient();

            client.ConnectionManager.Transport.GetType().Should().Be(typeof(WebSocketTransport));
        }

        [Trait("spec", "RTN2")]
        public class ConnectionParameterTests : ConnectionSpecs
        {

            public ConnectionParameterTests(ITestOutputHelper output) : base(output)
            {
            }
        }

        private AblyRealtime GetClient()
        {
            return new AblyRealtime(ValidKey);
        }
    }
}
