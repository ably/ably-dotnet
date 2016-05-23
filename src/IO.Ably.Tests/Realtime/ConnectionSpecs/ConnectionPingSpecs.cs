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
        [Fact]
        [Trait("intermittent", "true")]
        public async Task ShouldSendHeartbeatMessage()
        {
            var client = GetConnectedClient();

            var result = await client.Connection.PingAsync();

            LastCreatedTransport.LastMessageSend.action.Should().Be(ProtocolMessage.MessageAction.Heartbeat);
        }

        [Fact]
        [Trait("spec", "RTN13a")]
        public async Task OnHeartBeatMessageReceived_ShouldReturnElapsedTime()
        {
            Now = DateTimeOffset.UtcNow;
            var client = GetConnectedClient();

            _fakeTransportFactory.LastCreatedTransport.SendAction = async message =>
            {
                Now = Now.AddMilliseconds(100);
                if (message.Original.action == ProtocolMessage.MessageAction.Heartbeat)
                {
                    await Task.Delay(1);
                    await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
                }
            };
            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        [Trait("spec", "RTN13b")]
        public async Task WithClosedOrFailedConnectionStates_ShouldReturnError()
        {
            var client = GetClientWithFakeTransport();

            ((IConnectionContext)client.ConnectionManager).SetState(new ConnectionClosedState(client.ConnectionManager, new ErrorInfo()));

            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ConnectionHeartbeatRequest.DefaultError);

            ((IConnectionContext)client.ConnectionManager).SetState(new ConnectionFailedState(client.ConnectionManager, new ErrorInfo()));

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
            result.Error.statusCode.Should().Be(HttpStatusCode.RequestTimeout);
        }

        public ConnectionPingSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}