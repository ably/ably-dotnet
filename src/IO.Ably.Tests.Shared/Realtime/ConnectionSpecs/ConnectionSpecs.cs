﻿using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Shared.Realtime.ConnectionSpecs
{
    public class ConnectionSpecs : AblyRealtimeSpecs
    {
        [Fact]
        [Trait("spec", "RTN16i")]
        [Trait("spec", "RTN16f")]
        [Trait("spec", "RTN16j")]
        public async Task RecoveryKey_MsgSerialShouldNotBeSentToAblyButShouldBeSetOnConnection()
        {
            var recoveryKey =
                "{\"connectionKey\":\"uniqueKey\",\"msgSerial\":45,\"channelSerials\":{\"channel1\":\"1\",\"channel2\":\"2\",\"channel3\":\"3\"}}";
            FakeTransportFactory.InitialiseFakeTransport =
                transport => transport.OnConnectChangeStateToConnected = false;
            var client = GetClientWithFakeTransport(options => { options.Recover = recoveryKey; });

            await Task.Delay(9000);
            client.Connection.MessageSerial.Should().Be(45);

            var transportParams = await client.ConnectionManager.CreateTransportParameters("https://realtime.ably.io");
            var paramsDict = transportParams.GetParams();
            paramsDict.ContainsKey("recover").Should().BeTrue();
            paramsDict["recover"].Should().Be("uniqueKey");
            paramsDict.ContainsKey("msg_serial").Should().BeFalse();
        }

        public ConnectionSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
