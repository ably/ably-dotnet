using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
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
        [Trait("spec", "RTN16b")]
        public async Task RecoveryKey_ShouldBeConnectionKeyPlusConnectionSerialPlusMsgSerial()
        {
            var client = await GetConnectedClient();
            client.Connection.RecoveryKey.Should().Be($"{client.Connection.Key}:{client.Connection.Serial}:{client.Connection.MessageSerial}");
        }

        [Fact]
        [Trait("spec", "RTN16f")]
        public async Task RecoveryKey_MsgSerialShouldNotBeSentToAblyButShouldBeSetOnConnection()
        {
            // RecoveryKey should be in the format
            // LettersOrNumbers:Number:Number
            TransportParams.RecoveryKeyRegex.Match("a:b:c").Success.Should().BeFalse();
            TransportParams.RecoveryKeyRegex.Match("a:b:3").Success.Should().BeFalse();
            TransportParams.RecoveryKeyRegex.Match("a:2:c").Success.Should().BeFalse();
            TransportParams.RecoveryKeyRegex.Match("$1:2:3").Success.Should().BeFalse();
            TransportParams.RecoveryKeyRegex.Match("$a:2:3").Success.Should().BeFalse();
            TransportParams.RecoveryKeyRegex.Match("a:@2:3").Success.Should().BeFalse();
            TransportParams.RecoveryKeyRegex.Match("a:2:3!").Success.Should().BeFalse();

            // these should be valid
            TransportParams.RecoveryKeyRegex.Match("1:2:3").Success.Should().BeTrue();
            TransportParams.RecoveryKeyRegex.Match("a:2:3").Success.Should().BeTrue();

            const string recoveryKey = "abcxyz:100:99";
            var match = TransportParams.RecoveryKeyRegex.Match(recoveryKey);
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("abcxyz");
            match.Groups[2].Value.Should().Be("100");
            match.Groups[3].Value.Should().Be("99");

            var parts = recoveryKey.Split(':');

            var client = GetRealtimeClient(options => { options.Recover = recoveryKey; });

            var transportParams = await client.ConnectionManager.CreateTransportParameters("https://realtime.ably.io");
            var paramsDict = transportParams.GetParams();
            paramsDict.ContainsKey("recover").Should().BeTrue();
            paramsDict.ContainsKey("connection_serial").Should().BeTrue();
            paramsDict.ContainsKey("msg_serial").Should().BeFalse();

            paramsDict["recover"].Should().Be(parts[0]);
            paramsDict["connection_serial"].Should().Be(parts[1]);

            client.Connection.MessageSerial.Should().Be(99);
        }

        public ConnectionRecoverySpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
