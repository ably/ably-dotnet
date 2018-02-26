using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.AcceptanceTests;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class ClosedStateSpecs : AblySpecs
    {
        private FakeConnectionContext _context;
        private ConnectionClosedState _state;
        private DefaultLogger.InternalLogger _logger;

        public ClosedStateSpecs(ITestOutputHelper output)
            : base(output)
        {
            var sink = new TestLoggerSink();
            _logger = new DefaultLogger.InternalLogger(Defaults.DefaultLogLevel, sink);
            _context = new FakeConnectionContext();
            _state = new ConnectionClosedState(_context, _logger);
        }

        [Fact]
        public void ShouldHaveCorrectState()
        {
            _state.State.Should().Be(ConnectionState.Closed);
        }

        [Fact]
        public void WhenConnectCalled_MovesToConnectingState()
        {
            // Act
            _state.Connect();

            // Assert
            _context.StateShouldBe<ConnectionConnectingState>();
        }

        [Fact]
        public void WhenCloseCalled_ShouldDoNothing()
        {
            // Act
            new ConnectionClosedState(null, _logger).Close();
        }

        [Fact]
        public void BeforeTransition_ShouldDestroyTransport()
        {
            // Arrange
            _context.Transport = new FakeTransport();

            // Act
            _state.BeforeTransition();

            // Assert
            _context.DestroyTransportCalled.Should().BeTrue();
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
        public async Task ShouldNotHandleInboundMessageWithAction(ProtocolMessage.MessageAction action)
        {
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        [Trait("spec", "RTN7c")]
        [Trait("sandboxTest", "needed")]
        public async Task ShouldClearAckQueue()
        {
            // Arrange
            await _state.OnAttachToContext();

            _context.ClearAckQueueMessagesCalled.Should().BeTrue();
            _context.ClearAckMessagesError.Should().Be(ErrorInfo.ReasonClosed);
        }

        [Fact]
        public void ShouldClearConnectionKeyAndId()
        {
            // Arrange
            _context.Connection.Key = "test";

            // Act
            _state.BeforeTransition();

            // Assert
            _context.Connection.Key.Should().BeNullOrEmpty();
            _context.Connection.Id.Should().BeNullOrEmpty();
        }
    }
}
