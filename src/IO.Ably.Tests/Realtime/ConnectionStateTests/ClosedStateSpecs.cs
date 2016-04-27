using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class ClosedStateSpecs : AblySpecs
    {
        [Fact]
        public void ClosedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosedState state = new ConnectionClosedState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionStateType>(Ably.Realtime.ConnectionStateType.Closed, state.State);
        }

        [Fact]
        public void ClosedState_Connect_GoesToConnecting()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosedState state = new ConnectionClosedState(context.Object);

            // Act
            state.Connect();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }

        [Fact]
        public void ClosedState_Close_DoesNothing()
        {
            // Arrange
            ConnectionClosedState state = new ConnectionClosedState(null);

            // Act
            state.Close();
        }

        [Fact]
        public void ClosedState_SendMessage_DoesNothing()
        {
            // Arrange
            ConnectionClosedState state = new ConnectionClosedState(null);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));
        }

        [Fact]
        public void ClosedState_AttachToContext_DestroysTransport()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Connection(new Mock<IConnectionManager>().Object));
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.CreateTransport()).Callback(() =>
                context.Setup(c => c.Transport).Returns(transport.Object));
            ConnectionClosedState state = new ConnectionClosedState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.DestroyTransport(), Times.Once());
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
        public async Task ClosedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionClosedState state = new ConnectionClosedState(null);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ClosedState_DoesNotListenToTransportChanges()
        {
            // Arrange
            ConnectionClosedState state = new ConnectionClosedState(null);

            // Act
            state.OnTransportStateChanged(null);
        }

        [Fact]
        public void ClosedState_UpdatesConnectionInformation()
        {
            // Arrange

            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<Connection> target = new Mock<Connection>();
            target.SetupProperty(c => c.Key, "test test");
            context.SetupGet(c => c.Connection).Returns(target.Object);
            ConnectionClosedState state = new ConnectionClosedState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            target.VerifySet(c => c.Key = null);
        }
        public ClosedStateSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}