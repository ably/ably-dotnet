using Moq;
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
    public class FailedStateSpecs : AblySpecs
    {
        private FakeConnectionContext _context;
        private ConnectionFailedState _state;

        private ConnectionFailedState GetState(ErrorInfo info = null)
        {
            return new ConnectionFailedState(_context, info, Logger);
        }

        public FailedStateSpecs(ITestOutputHelper output) : base(output)
        {
            _context = new FakeConnectionContext();
            _state = GetState();
        }

        [Fact]
        public void ShouldHaveCorrectState()
        {
            // Arrange
            _state.State.Should().Be(ConnectionState.Failed);
        }

        [Fact]
        public void ConnectCalled_ShouldGoToConnecting()
        {
            // Act
            _state.Connect();

            // Assert
            _context.StateShouldBe<ConnectionConnectingState>();
        }

        [Fact]
        public void Close_ShouldDoNothing()
        {
            // Act
            _state.Close();
        }

        [Fact]
        public void BeforeTransition_DestroysTransport()
        {
            // Arrange
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
        public async Task ShouldNotHandleInboundMessages(ProtocolMessage.MessageAction action)
        {
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void BeforeTransition_ShouldClearConnectionKeyAndId()
        {
            // Arrange
            _context.Connection.Key = "Test";
            _context.Connection.Id = "Test";

            // Act
            _state.BeforeTransition();

            // Assert
            _context.Connection.Key.Should().BeNullOrEmpty();
            _context.Connection.Id.Should().BeNullOrEmpty();
        }

        [Fact]
        [Trait("spec", "RTN7c")]
        [Trait("sandboxTest", "needed")]
        public async Task OnAttached_ClearsAckQueue()
        {
            // Arrange
            await _state.OnAttachToContext();

            _context.ClearAckQueueMessagesCalled.Should().BeTrue();
            _context.ClearAckMessagesError.Should().Be(ErrorInfo.ReasonFailed);
        }
    }
}
