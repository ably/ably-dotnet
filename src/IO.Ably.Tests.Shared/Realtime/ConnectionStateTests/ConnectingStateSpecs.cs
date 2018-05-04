using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class ConnectingStateSpecs : AblySpecs
    {
        private FakeConnectionContext _context;
        private ConnectionConnectingState _state;
        private FakeTimer _timer;

        public ConnectingStateSpecs(ITestOutputHelper output)
            : base(output)
        {
            _context = new FakeConnectionContext();
            _timer = new FakeTimer();
            _state = new ConnectionConnectingState(_context, _timer, Logger);
        }

        private static FakeTransport GetConnectedTrasport()
        {
            return new FakeTransport() { State = TransportState.Connected };
        }

        [Fact]
        public void HasCorrectState()
        {
            _state.State.Should().Be(Ably.Realtime.ConnectionState.Connecting);
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Attached)]
        [InlineData(ProtocolMessage.MessageAction.Close)]
        [InlineData(ProtocolMessage.MessageAction.Closed)]
        [InlineData(ProtocolMessage.MessageAction.Connect)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Detached)]
        [InlineData(ProtocolMessage.MessageAction.Disconnect)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public async Task ShouldNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            bool result = await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldHandleInboundConnectedMessage()
        {
            _context.Transport = new FakeTransport() { State = TransportState.Connecting };

            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WithInboundConnectedMessageAndClosingTrasport_ShouldNotGoToConnected()
        {
            // Arrange
            _context.Transport = new FakeTransport() { State = TransportState.Closing };

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            _context.LastSetState.Should().BeNull();
        }

        [Fact]
        public async Task WithConnectedTransportAndInboundConnectedMessage_ShouldGoToConnected()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionConnectedState>();
        }

        [Fact]
        public async Task ConnectingState_HandlesInboundErrorMessage()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WithHandlesInboundErrorMessage_GoesToDisconnected()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();
            _context.CanUseFallBack = false;
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = targetError });

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionDisconnectedState>();
        }

        [Fact]
        public async Task WithInboundErrorMessageWhenItCanUseFallBack_ShouldCallHandleConnectionFailure()
        {
            _context.Transport = new FakeTransport() { State = TransportState.Connected };
            _context.CanUseFallBack = true;

            // Arrange
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = targetError });

            // Assert
            _context.HandledConnectionFailureCalled.Should().BeTrue();
        }

        [Fact]
        public async Task WithInboundErrorMessageMessageWhenItCanUseFallBack_ShouldClearsConnectionKey()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();
            _context.CanUseFallBack = true;
            _context.Connection.Key = "123";

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = new ErrorInfo("test", 123, System.Net.HttpStatusCode.InternalServerError) });

            // Assert
            _context.Connection.Key.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task WithInboundDisconnectedMessage_ShouldLetConnectionManagerHandleTheDisconnect()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            _context.HandledConnectionFailureCalled.Should().BeTrue();
        }

        [Fact]
        public void Connect_ShouldDoNothing()
        {
            // Act
            _state.Connect();
        }

        [Fact]
        public void Close_ShouldGoToClosing()
        {
            // Act
            _state.Close();

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionClosingState>();
        }

        [Fact]
        public async Task OnAttachedToContext_CreatesTransport()
        {
            _context.AllowTransportCreating = true;

            // Act
            await _state.OnAttachToContext();

            // Assert
            _context.CreateTransportCalled.Should().BeTrue();
        }

        [Fact]
        public async Task ConnectingState_ForceDisconnect()
        {
            // Arrange
            _context.Transport = new FakeTransport() { State = TransportState.Initialized };

            // Act
            await _state.OnAttachToContext();
            _timer.OnTimeOut();

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _context.HandledConnectionFailureCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        public async Task WhenMessageReceived_ForceDisconnectNotAppliedAndTimerShouldBeAborted(ProtocolMessage.MessageAction action)
        {
            // Arrange
            var transport = new FakeTransport() { State = TransportState.Initialized };
            _context.Transport = transport;

            // Act
            await _state.OnAttachToContext();
            transport.State = TransportState.Connected;
            await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _timer.Aborted.Should().BeTrue();
        }
    }
}
