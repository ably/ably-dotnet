using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class SuspendedStateSpecs : AblySpecs
    {
        private FakeConnectionContext _context;
        private ConnectionSuspendedState _state;
        private FakeTimer _timer;

        private ConnectionSuspendedState GetState(ErrorInfo info = null)
        {
            return new ConnectionSuspendedState(_context, info, _timer, Logger);
        }

        public SuspendedStateSpecs(ITestOutputHelper output)
            : base(output)
        {
            _timer = new FakeTimer();
            _context = new FakeConnectionContext();
            _state = GetState();
        }

        [Fact]
        public void ShouldHaveSuspendedState()
        {
            _state.State.Should().Be(ConnectionState.Suspended);
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Attached)]
        [InlineData(ProtocolMessage.MessageAction.Close)]
        [InlineData(ProtocolMessage.MessageAction.Closed)]
        [InlineData(ProtocolMessage.MessageAction.Connect)]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Detached)]
        [InlineData(ProtocolMessage.MessageAction.Disconnect)]
        [InlineData(ProtocolMessage.MessageAction.Disconnected)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public async Task ShouldIgnoreInboundMessages(ProtocolMessage.MessageAction action)
        {
            // Act
            var result = await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        [Trait("spec", "RTN12d")]
        public void Close_ChangesStateToClosedAndAbortsTimer()
        {
            // Act
            _state.Close();

            // Assert
            _context.StateShouldBe<ConnectionClosedState>();
            _timer.Aborted.Should().BeTrue();
        }

        [Fact]
        public void Connect_ShouldChangeStateToConnecting()
        {
            // Act
            _state.Connect();

            // Assert
            _context.StateShouldBe<ConnectionConnectingState>();
        }

        [Fact]
        [Trait("spec", "RTN14e")]
        public async Task ShouldRetyConnection()
        {
            _context.Transport = new FakeTransport(TransportState.Initialized);

            // Act
            await _state.OnAttachToContext();
            _timer.StartedWithAction.Should().BeTrue();
            _timer.OnTimeOut();

            // Assert
            _context.StateShouldBe<ConnectionConnectingState>();
        }

        [Fact]
        [Trait("spec", "RTN7c")]
        [Trait("sandboxTest", "needed")]
        public async Task OnAttached_ClearsAckQueue()
        {
            // Arrange
            await _state.OnAttachToContext();

            _context.ClearAckQueueMessagesCalled.Should().BeTrue();
            _context.ClearAckMessagesError.Should().Be(ErrorInfo.ReasonSuspended);
        }
    }
}
