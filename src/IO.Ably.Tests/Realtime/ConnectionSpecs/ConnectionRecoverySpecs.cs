using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime.ConnectionSpecs
{
    public class ConnectionRecoverySpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN16c")]
        public async Task WhenConnectionIsClosed_ConnectionIdAndKeyShouldBeReset()
        {
            var client = GetConnectedClient();
            
            client.Close();

            await client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            client.Connection.State.Should().Be(ConnectionStateType.Closed);
            client.Connection.Id.Should().BeNullOrEmpty();
            client.Connection.Key.Should().BeNullOrEmpty();
        }

        [Fact]
        [Trait("spec", "RTN16b")]
        public void RecoveryKey_ShouldBeConnectionKeyPlusConnectionSerial()
        {
            var client = GetConnectedClient();
            client.Connection.RecoveryKey.Should().Be($"{client.Connection.Key}:{client.Connection.Serial}");
        }

        public ConnectionRecoverySpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}