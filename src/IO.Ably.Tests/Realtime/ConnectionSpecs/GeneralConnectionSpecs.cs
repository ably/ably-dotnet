using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class GeneralConnectionSpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN1")]
        public void ShouldUseWebSocketTransport()
        {
            var client = GetRealtimeClient();

            client.ConnectionManager.Transport.GetType().Should().Be(typeof(WebSocketTransport));
        }

        [Fact]
        [Trait("spec", "RTN3")]
        [Trait("spec", "RTN6")]
        public void WithAutoConnect_CallsConnectOnTransport()
        {
            var client = GetClientWithFakeTransport(opts => opts.AutoConnect = true);
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            client.ConnectionManager.ConnectionState.Should().Be(ConnectionStateType.Connected);
            LastCreatedTransport.ConnectCalled.Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "RTN3")]
        public void WithAutoConnectFalse_LeavesStateAsInitialized()
        {
            var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);

            client.ConnectionManager.ConnectionState.Should().Be(ConnectionStateType.Initialized);
            LastCreatedTransport.Should().BeNull("Transport shouldn't be created without calling connect when AutoConnect is false");
        }

        [Fact]
        [Trait("spec", "RTN19")]
        public void WhenConnectedMessageReceived_ConnectionShouldBeInConnectedStateAndConnectionDetailsAreUpdated()
        {
            var client = GetClientWithFakeTransport();

            var connectionDetailsMessage = new ConnectionDetailsMessage()
            {
                clientId = "123",
                connectionKey = "boo"
            };
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                connectionDetails = connectionDetailsMessage,
                connectionKey = "unimportant"
            });

            client.Connection.State.Should().Be(ConnectionStateType.Connected);
            client.Connection.Key.Should().Be("boo");
        }

        [Fact]
        [Trait("spec", "RTN19")]
        public void WhenConnectedMessageReceived_WithNoConnectionDetailsButConnectionKeyInMessage_ShouldHaveCorrectKey()
        {
            var client = GetClientWithFakeTransport();

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                connectionKey = "unimportant"
            });

            client.Connection.Key.Should().Be("unimportant");
        }

        [Fact]
        [Trait("spec", "RSA15a")]
        [Trait("sandboxTest", "needed")]
        public void WhenConnectedMessageReceivedWithClientId_AblyAuthShouldUseConnectionClientId()
        {
            var client = GetClientWithFakeTransport();

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                connectionDetails = new ConnectionDetailsMessage { clientId = "realtimeClient" }
            });

            client.RestClient.AblyAuth.GetClientId().Should().Be("realtimeClient");
        }

        public GeneralConnectionSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}