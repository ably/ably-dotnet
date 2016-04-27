using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class ConnectingStateSpecs : ConnectionStatesTests
    {
        private FakeConnectionContext _context;
        private ConnectionConnectingState _state;
        private FakeTimer _timer;


        public ConnectingStateSpecs(ITestOutputHelper output) : base(output)
        {
            _context = new FakeConnectionContext();
            _timer = new FakeTimer();
            _state = new ConnectionConnectingState(_context, _timer);
        }

        private static FakeTransport GetConnectedTrasport()
        {
            return new FakeTransport() { State = TransportState.Connected };
        }



        [Fact]
        public void HasCorrectState()
        {
            _state.State.Should().Be(Ably.Realtime.ConnectionStateType.Connecting);
        }

        [Fact]
        public async Task OnAttachedToContext_ShouldAttempToConnect()
        {
            _state.OnAttachedToContext();

            _context.AttempConnectionCalled.Should().BeTrue();
        }

        [Fact]
        public void SendMessage_ShouldQueueMessage()
        {
            _state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Connect));

            _context.QueuedMessages.Should().HaveCount(1);
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
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connected);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WithHandlesInboundErrorMessage_GoesToFailed()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionFailedState>();
        }


        [Theory]
        [InlineData(System.Net.HttpStatusCode.InternalServerError)]
        [InlineData(System.Net.HttpStatusCode.GatewayTimeout)]
        public async Task WithInboundErrorMessage_GoesToDisconnected(System.Net.HttpStatusCode code)
        {
            _context.Transport = new FakeTransport() { State = TransportState.Connected };
            // Arrange
            ErrorInfo targetError = new ErrorInfo("test", 123, code);

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionDisconnectedState>();
        }

        [Fact]
        public async Task WithInboundErrorMessage_ShouldClearsConnectionKey()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();
            _context.Connection.Key = "123";

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("test", 123, System.Net.HttpStatusCode.InternalServerError) });

            // Assert
            _context.Connection.Key.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task WithInboundDisconnectedMessage_ShouldMoveToDisconnectedState()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            _context.LastSetState.Should().BeOfType<ConnectionDisconnectedState>();
        }

        [Theory]
        [InlineData(System.Net.HttpStatusCode.InternalServerError)]
        [InlineData(System.Net.HttpStatusCode.GatewayTimeout)]
        public async Task ConnectingState_HandlesInboundDisconnectedMessage_GoesToDisconnected_FallbackHost(System.Net.HttpStatusCode code)
        {
            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { error = new ErrorInfo("", 0, code) });

            // Assert
            var lastType = _context.LastSetState as ConnectionDisconnectedState;
            lastType.UseFallbackHost.Should().BeTrue();
        }

        [Fact]
        public async Task WithInboundDisconnectedMessageAndFirstAttempIsMoreThanTimeoutValue_GoesToSuspended()
        {
            // Arrange
            _context.FirstConnectionAttempt = Now.AddHours(-1);

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionSuspendedState>();
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
            // Act
            await _state.OnAttachedToContext();

            // Assert
            _context.CreateTransportCalled.Should().BeTrue();
        }

        [Fact]
        public async Task OnAttachedToContext_WithClosedTransport_ShouldConnectTheTransport()
        {
            // Arrange
            var fakeTransport = new FakeTransport() { State = TransportState.Closed };
            _context.Transport = fakeTransport;

            // Act
            await _state.OnAttachedToContext();

            // Assert
            fakeTransport.ConnectCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connected)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public async Task WhenTransportStateChanges_ShouldNotSwitchState(TransportState transportState)
        {
            // Act
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(transportState));

            // Assert
            _context.LastSetState.Should().BeNull();
        }

        [Fact]
        public async Task WhenTransportGoesDisconnected_ShouldSwitchToDisconnected()
        {
            // Act
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionDisconnectedState>();
        }

        [Fact]
        public async Task TransportGoesDisconnectedWithError_ShouldSwitchToDisconnected()
        {
            // Act
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed, new Exception()));

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionDisconnectedState>();
        }

        [Fact]
        public async Task WhenTransportGoesDisconnected_SwitchesToSuspended()
        {
            // Arrange
            _context.FirstConnectionAttempt = Now.AddHours(-1);

            // Act
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            _context.LastSetState.Should().BeOfType<ConnectionSuspendedState>();
        }

        [Fact]
        public async Task ConnectingState_ForceDisconnect()
        {
            // Arrange
            _context.Transport = new FakeTransport() { State = TransportState.Initialized};

            // Act
            await _state.OnAttachedToContext();
            _timer.OnTimeOut();

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _context.LastSetState.Should().BeOfType<ConnectionDisconnectedState>();
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Disconnected)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        public async Task WhenMessageReceived_ForceDisconnectNotAppliedAndTimerShouldBeAborted(ProtocolMessage.MessageAction action)
        {
            // Arrange
            var transport = new FakeTransport() { State = TransportState.Initialized };
            _context.Transport = transport;

            // Act
            await _state.OnAttachedToContext();
            transport.State = TransportState.Connected;
            await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _timer.Aborted.Should().BeTrue();
        }

        [Fact]
        public async Task ConnectingState_ForceDisconnectNotApplied_WhenTransportClosed()
        {
            // Arrange
            _context.Transport = new FakeTransport() { State = TransportState.Initialized };

            // Act
            await _state.OnAttachedToContext();
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _timer.Aborted.Should().BeTrue();
        }

    }
}