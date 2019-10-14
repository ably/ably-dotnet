using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN7")]
    public sealed class AckNackSpecs : AblyRealtimeSpecs
    {
        private AblyRealtime _realtime;

        // TODO: Find a way to test

        [Fact(Skip="TODO find a way to test")]
        public async Task WhemMessageReceived_ShouldPassTheMessageThroughTheAckProcessor()
        {
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Ack);
            // _realtime.ConnectionManager.OnTransportMessageReceived(message);

            await Task.Delay(100); // Let the execution complete

            // _ackProcessor.OnMessageReceivedCalled.Should().BeTrue();
        }

        public AckNackSpecs(ITestOutputHelper output)
            : base(output)
        {
            _realtime = GetRealtimeClient();
        }
    }
}
