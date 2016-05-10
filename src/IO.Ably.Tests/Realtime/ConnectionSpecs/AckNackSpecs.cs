using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN7")]
    public class AckNackSpecs : ConnectionSpecsBase
    {
        private AblyRealtime _realtime;
        private FakeAckProcessor _ackProcessor;
        // This only contains the AckProcessor integration with the ConnectionManager. 
        // The Actual Ack processor tests are in AckProtocolSpecs.cs

        [Fact]
        public void ShouldListenToConnectionStateChanges()
        {
            _realtime.ConnectionManager.SetState(
                new ConnectionFailedState(_realtime.ConnectionManager, new ErrorInfo()));

            _ackProcessor.OnStatecChanged.Should().BeTrue();
            _ackProcessor.LastState.Should().BeOfType<ConnectionFailedState>();
        }

        [Fact]
        public void WhenSendIsCalled_ShouldPassTheMessageThroughTHeAckProcessor()
        {
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            _realtime.ConnectionManager.Send(message, null);
            _ackProcessor.SendMessageCalled.Should().BeTrue();
        }

        [Fact]
        public async Task WhemMessageReceived_ShouldPassTheMessageThroughTheAckProcessor()
        {
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Ack);
            await _realtime.ConnectionManager.OnTransportMessageReceived(message);

            _ackProcessor.OnMessageReceivedCalled.Should().BeTrue();
        }

        public AckNackSpecs(ITestOutputHelper output) : base(output)
        {
            _ackProcessor = new FakeAckProcessor();
            _realtime = GetRealtimeClient();
            _realtime.ConnectionManager.AckProcessor = _ackProcessor;
        }
    }
}