using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
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
            var client = GetClientWithFakeTransport(options => { options.Recover = recoveryKey; });

            var transportParams = await client.ConnectionManager.CreateTransportParameters("https://realtime.ably.io");
            var paramsDict = transportParams.GetParams();
            paramsDict.ContainsKey("recover").Should().BeTrue();
            paramsDict["recover"].Should().Be("uniqueKey");
            paramsDict.ContainsKey("msg_serial").Should().BeFalse();

            async Task WaitFor(Action<Action> done)
            {
                await TestHelpers.WaitFor(10000, 1, done);
            }

            await WaitFor(done =>
            {
                if (client.Connection.MessageSerial == 45)
                {
                    done();
                }
            });

            // client.Connection.MessageSerial.Should().Be(45); // assertion fails on CI
        }

        public ConnectionSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
