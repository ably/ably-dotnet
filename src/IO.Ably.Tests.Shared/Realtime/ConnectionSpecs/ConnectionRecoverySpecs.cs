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

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            client.Connection.State.Should().Be(ConnectionState.Closed);
            client.Connection.Id.Should().BeNullOrEmpty();
            client.Connection.Key.Should().BeNullOrEmpty();
        }

        [Fact]
        [Trait("spec", "RTN16b")]
        public void RecoveryKey_ShouldBeConnectionKeyPlusConnectionSerialPlusMsgSerial()
        {
            var client = GetConnectedClient();
            client.Connection.Serial.Should().Be()
            client.Connection.RecoveryKey.Should().Be($"{client.Connection.Key}:{client.Connection.Serial}:{client.Connection.MessageSerial}");
        }

        public ConnectionRecoverySpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
