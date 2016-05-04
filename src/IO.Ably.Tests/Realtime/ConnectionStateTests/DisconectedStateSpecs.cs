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
    public class DisconectedStateSpecs : AblySpecs
    {
        private FakeConnectionContext _context;
        private ConnectionInfo _connectionInfo;
        private ConnectionDisconnectedState _state;
        private FakeTimer _timer;

        public DisconectedStateSpecs(ITestOutputHelper output) : base(output)
        {
            _context = new FakeConnectionContext();
            _connectionInfo = new ConnectionInfo("", 0, "", "");
            _timer = new FakeTimer();
            _state = GetState();
        }

        private ConnectionDisconnectedState GetState(ConnectionState.TransportStateInfo stateInfo)
        {
            return new ConnectionDisconnectedState(_context, stateInfo);
        }

        private ConnectionDisconnectedState GetState(ErrorInfo error = null, ICountdownTimer timer = null)
        {
            return new ConnectionDisconnectedState(_context, error, _timer);
        }

        [Fact]
        public void ShouldHaveDisconnectedTypes()
        {
            _state.State.Should().Be(ConnectionStateType.Disconnected);
        }

        [Fact]
        public void ShouldQueueMessages()
        {
            // Act
            _state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Connect));

            // Assert
            _context.QueuedMessages.Should().HaveCount(1);
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
        public async Task ShouldNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            bool handled = await state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            handled.Should().BeFalse();
        }

        [Fact]
        public async Task SHouldNotListenToTransportStateChanges()
        {
            // Arrange
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            await state.OnTransportStateChanged(null);
        }

        [Fact]
        [Trait("spec", "RTN12d")]
        public void WhenCloseCalled_ShouldTrasitionToClosedAndTimerAborted()
        {
            // Arrange
            
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            state.Close();

            // Assert
            _context.StateShouldBe<ConnectionClosedState>();
            _timer.Aborted.Should().BeTrue();
        }

        [Fact]
        public void WhenConnectCalled_SHouldTrasitionToConnecting()
        {
            // Arrange
            
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            state.Connect();

            // Assert
            _context.StateShouldBe<ConnectionConnectingState>();
        }

        [Fact]
        public async Task AfterAnInterval_ShouldRetryConnection()
        {
            // Arrange
            var transport = new FakeTransport() { State = TransportState.Initialized };
            _context.Transport = transport;
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            await state.OnAttachedToContext();
            _timer.OnTimeOut();

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _context.StateShouldBe<ConnectionConnectingState>();
        }

        [Fact]
        public async Task WhenDisconnectedWithFallback_ShouldRetryConnectionImmediately()
        {
            // Arrange
            // Arrange
            var transport = new FakeTransport() { State = TransportState.Initialized };
            _context.Transport = transport;
            var state = GetState(ErrorInfo.ReasonClosed);
            state.UseFallbackHost = true;

            // Act
            await state.OnAttachedToContext();

            // Assert
            _timer.StartedWithAction.Should().BeFalse();
            _context.StateShouldBe<ConnectionConnectingState>();
        }
    }
}