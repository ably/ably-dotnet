using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Shared.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime.ConnectionSpecs
{
    public class ConnectionRecoverySpecs : AblyRealtimeSpecs
    {
        [Fact]
        [Trait("spec", "RTN16c")]
        public async Task WhenConnectionIsClosed_ConnectionIdAndKeyShouldBeReset()
        {
            var client = await GetConnectedClient();

            client.Close();

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            await client.WaitForState(ConnectionState.Closed);
            client.Connection.Id.Should().BeNullOrEmpty();
            client.Connection.Key.Should().BeNullOrEmpty();
        }

        [Fact]
        [Trait("spec", "RTN16g")]
        public async Task RecoveryKey_ShouldContainSerializedConnectionKeyAndConnectionSerialAndMsgSerial()
        {
            var client = await GetConnectedClient();
            var expectedRecoveryKey = new RecoveryKeyContext()
            {
                ConnectionKey = client.Connection.Key,
                MsgSerial = client.Connection.MessageSerial,
                ChannelSerials = client.Channels.GetChannelSerials(),
            }.Encode();
            client.Connection.RecoveryKey.Should().Be(expectedRecoveryKey);
        }

        [Fact]
        [Trait("spec", "RTN16i")]
        [Trait("spec", "RTN16f")]
        [Trait("spec", "RTN16j")]
        public async Task RecoveryKey_MsgSerialShouldNotBeSentToAblyButShouldBeSetOnConnection()
        {
            var recoveryKey =
                "{\"connectionKey\":\"uniqueKey\",\"msgSerial\":45,\"channelSerials\":{\"channel1\":\"1\",\"channel2\":\"2\",\"channel3\":\"3\"}}";
            var client = GetRealtimeClient(options => { options.Recover = recoveryKey; });

            var transportParams = await client.ConnectionManager.CreateTransportParameters("https://realtime.ably.io");
            var paramsDict = transportParams.GetParams();
            paramsDict.ContainsKey("recover").Should().BeTrue();
            paramsDict.ContainsKey("msg_serial").Should().BeFalse();
            paramsDict["recover"].Should().Be("uniqueKey");
        }

        public ConnectionRecoverySpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
