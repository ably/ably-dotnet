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
    public sealed class AckNackSpecs : ConnectionSpecsBase
    {
        private AblyRealtime _realtime;
        private FakeAckProcessor _ackProcessor;

        // This only contains the AckProcessor integration with the ConnectionManager.
        // The Actual Ack processor tests are in AckProtocolSpecs.cs
        [Fact]
        public void WhenSendIsCalled_ShouldPassTheMessageThroughTHeAckProcessor()
        {
            _realtime.Connection.ConnectionState = new ConnectionConnectedState(
                _realtime.ConnectionManager,
                new ConnectionInfo(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)));

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            _realtime.ConnectionManager.Send(message, null);
            _ackProcessor.QueueIfNecessaryCalled.Should().BeTrue();
        }

        [Fact]
        public async Task WhemMessageReceived_ShouldPassTheMessageThroughTheAckProcessor()
        {
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Ack);
            await _realtime.ConnectionManager.OnTransportMessageReceived(message);

            _ackProcessor.OnMessageReceivedCalled.Should().BeTrue();
        }

        public AckNackSpecs(ITestOutputHelper output)
            : base(output)
        {
            _ackProcessor = new FakeAckProcessor();
            _realtime = GetRealtimeClient();
            _realtime.ConnectionManager.AckProcessor = _ackProcessor;
        }
    }
}