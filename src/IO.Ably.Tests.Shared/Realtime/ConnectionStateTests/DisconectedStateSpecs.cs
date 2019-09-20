using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
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
        private RealtimeState EmptyState = new RealtimeState();

        public DisconectedStateSpecs(ITestOutputHelper output)
            : base(output)
        {
            _context = new FakeConnectionContext();
            _connectionInfo = new ConnectionInfo(string.Empty, 0, string.Empty, string.Empty);
            _timer = new FakeTimer();
            _state = GetState();
        }

        private ConnectionDisconnectedState GetState(ErrorInfo error = null, ICountdownTimer timer = null)
        {
            return new ConnectionDisconnectedState(_context, error, _timer, Logger);
        }

        [Fact]
        public void ShouldHaveDisconnectedTypes()
        {
            _state.State.Should().Be(ConnectionState.Disconnected);
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
            bool handled = await state.OnMessageReceived(new ProtocolMessage(action), EmptyState);

            // Assert
            handled.Should().BeFalse();
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
            await state.OnAttachToContext();
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
            state.RetryInstantly = true;

            // Act
            await state.OnAttachToContext();

            // Assert
            _timer.StartedWithAction.Should().BeFalse();
            _context.StateShouldBe<ConnectionConnectingState>();
        }
    }
}
