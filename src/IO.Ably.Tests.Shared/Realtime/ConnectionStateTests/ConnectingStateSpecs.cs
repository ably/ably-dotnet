using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime.Workflow;
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
            bool result = await _state.OnMessageReceived(new ProtocolMessage(action), null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShouldHandleInboundConnectedMessage()
        {
            _context.Transport = new FakeTransport() { State = TransportState.Connecting };

            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected), null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WithInboundConnectedMessageAndClosingTrasport_ShouldNotGoToConnected()
        {
            // Arrange
            _context.Transport = new FakeTransport() { State = TransportState.Closing };

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected), null);

            // Assert
            _context.LastSetState.Should().BeNull();
        }

        [Fact]
        public async Task WithConnectedTransportAndInboundConnectedMessage_ShouldGoToConnected()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();

            // Act
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected), null);

            // Assert
            _context.ShouldQueueCommand<SetConnectedStateCommand>();
        }

        [Fact]
        public async Task ConnectingState_HandlesInboundErrorMessage()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error), null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        [Trait("spec", "RTN14g")]
        public async Task WithInboundErrorMessage_WhenNotTokenErrorAndChannelsEmpty_GoesToFailed()
        {
            // Arrange
            _context.Transport = GetConnectedTrasport();
            _context.CanUseFallBack = false;
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = targetError }, null);

            // Assert
            _context.ShouldQueueCommand<SetFailedStateCommand>();
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
            _context.ShouldQueueCommand<SetClosingStateCommand>();
        }

        [Fact]
        public async Task ConnectingState_SendsHandleConnectionFailureCommand()
        {
            // Act
            _state.StartTimer();
            _timer.OnTimeOut();

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _context.ShouldQueueCommand<HandleConnectingFailureCommand>();
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
            _state.StartTimer();
            transport.State = TransportState.Connected;
            await _state.OnMessageReceived(new ProtocolMessage(action), null);

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _timer.Aborted.Should().BeTrue();
        }
    }
}
