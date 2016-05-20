using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN9")]
    public class ConnectionKeySpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN9a")]
        public void UntilConnected_ShouldBeNull()
        {
            var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);
            client.Connection.Key.Should().BeNullOrEmpty();
        }

        [Fact]
        [Trait("spec", "RTN9b")]
        [Trait("sandboxTest", "needed")]
        public void OnceConnected_ShouldUseKeyFromConnectedMessage()
        {
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected) { connectionDetails = new ConnectionDetailsMessage() { connectionKey = "key" } });
            client.Connection.Key.Should().Be("key");
        }

        [Fact]
        [Trait("spec", "RTN9b")]
        public async Task WhenRestoringConnection_UsesConnectionKey()
        {
            // Arrange
            string targetKey = "1234567";
            var client = GetClientWithFakeTransport();
            client.Connection.Key = targetKey;

            // Act
            var transportParamsForReconnect = await client.ConnectionManager.CreateTransportParameters();

            // Assert
            transportParamsForReconnect
                .ConnectionKey.Should().Be(targetKey);
        }

        public ConnectionKeySpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}