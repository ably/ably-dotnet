using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN13")]
    public class ConnectionPingSpecs : ConnectionSpecsBase
    {
        // [Fact]
        // [Trait("intermittent", "true")]
        // public async Task ShouldSendHeartbeatMessage()
        // {
        //    var client = GetConnectedClient();

        // var result = await client.Connection.PingAsync();

        // LastCreatedTransport.LastMessageSend.action.Should().Be(ProtocolMessage.MessageAction.Heartbeat);
        // }
        [Fact]
        [Trait("spec", "RTN13a")]
        public async Task OnHeartBeatMessageReceived_ShouldReturnElapsedTime()
        {
            SetNowFunc(() => DateTimeOffset.UtcNow);
            var client = GetConnectedClient();

            FakeTransportFactory.LastCreatedTransport.SendAction = async message =>
            {
                NowAdd(TimeSpan.FromMilliseconds(100));
                if (message.Original.Action == ProtocolMessage.MessageAction.Heartbeat)
                {
                    await Task.Delay(1);
                    await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
                }
            };
            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeTrue();

            // Because the now object is static when executed in parallel with other tests the results are affected
            result.Value.Value.Should().BeGreaterThan(TimeSpan.FromMilliseconds(0));

            // reset
            SetNowFunc(() => DateTimeOffset.UtcNow);
        }

        [Fact]
        [Trait("spec", "RTN13b")]
        public async Task WithClosedOrFailedConnectionStates_ShouldReturnError()
        {
            var client = GetClientWithFakeTransport();

            await client.ConnectionManager.SetState(new ConnectionClosedState(client.ConnectionManager, new ErrorInfo(), Logger));

            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ConnectionHeartbeatRequest.DefaultError);

            await ((IConnectionContext)client.ConnectionManager).SetState(new ConnectionFailedState(client.ConnectionManager, new ErrorInfo(), Logger));

            var resultFailed = await client.Connection.PingAsync();

            resultFailed.IsSuccess.Should().BeFalse();
            resultFailed.Error.Should().Be(ConnectionHeartbeatRequest.DefaultError);
        }

        [Fact]
        [Trait("spec", "RTN13c")]
        public async Task WhenDefaultTimeoutExpiresWithoutReceivingHeartbeatMessage_ShouldFailWithTimeoutError()
        {
            var client = GetConnectedClient(opts => opts.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100));

            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
        }

        public ConnectionPingSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
