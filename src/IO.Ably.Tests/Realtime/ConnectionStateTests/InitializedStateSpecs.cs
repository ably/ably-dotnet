using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class InitializedStateSpecs : AblySpecs
    {
        private FakeConnectionContext _context;
        private ConnectionInitializedState _state;

        #region Initialized
        public InitializedStateSpecs(ITestOutputHelper output) : base(output)
        {
            _context = new FakeConnectionContext();
            _state = new ConnectionInitializedState(_context);
        }

        [Fact]
        public void InitializedState_CorrectState()
        {
            // Assert
            _state.State.Should().Be(ConnectionStateType.Initialized);
        }

        [Fact]
        public void ShouldQueueMessagesWhenSent()
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
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CloseShouldDoNothing()
        {
            // Act
            _state.Close();
        }

        [Fact]
        public void OnConnect_ShouldGoToConnectionState()
        {
            // Act
            _state.Connect();

            // Assert
            _context.State.Should().BeOfType<ConnectionConnectingState>();
        }
        #endregion
    }
}