using FluentAssertions;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN8")]
    public class ConnectionIdSpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN8a")]
        public void ConnectionIdIsNull_WhenClientIsNotConnected()
        {
            var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);
            client.Connection.Id.Should().BeNullOrEmpty();
        }

        [Fact]
        [Trait("spec", "RTN8b")]
        [Trait("sandboxTest", "needed")]
        public void ConnectionIdSetBasedOnValueProvidedByAblyService()
        {
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected) { ConnectionId = "123" });
            client.Connection.Id.Should().Be("123");
        }

        public ConnectionIdSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}