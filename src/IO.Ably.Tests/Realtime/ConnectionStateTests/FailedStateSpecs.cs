using Moq;
using System.Threading.Tasks;
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
        public FailedStateSpecs(ITestOutputHelper output) : base(output)
        {

        }

        [Fact]
        public void FailedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionFailedState state = new ConnectionFailedState(context.Object, ErrorInfo.ReasonNeverConnected);

            // Assert
            Assert.Equal(Ably.Realtime.ConnectionStateType.Failed, state.State);
        }

        [Fact]
        public void FailedState_Connect_GoesToConnecting()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionFailedState state = new ConnectionFailedState(context.Object, ErrorInfo.ReasonNeverConnected);

            // Act
            state.Connect();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }

        [Fact]
        public void FailedState_Close_DoesNothing()
        {
            // Arrange
            ConnectionFailedState state = new ConnectionFailedState(null, ErrorInfo.ReasonNeverConnected);

            // Act
            state.Close();
        }

        [Fact]
        public void FailedState_SendMessage_DoesNothing()
        {
            // Arrange
            ConnectionFailedState state = new ConnectionFailedState(null, ErrorInfo.ReasonNeverConnected);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));
        }

        [Fact]
        public void FailedState_AttachToContext_DestroysTransport()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Connection(new Mock<IConnectionManager>().Object));
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.CreateTransport()).Callback(() =>
                context.Setup(c => c.Transport).Returns(transport.Object));
            ConnectionFailedState state = new ConnectionFailedState(context.Object, ErrorInfo.ReasonNeverConnected);

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
        public async Task FailedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionFailedState state = new ConnectionFailedState(null, ErrorInfo.ReasonNeverConnected);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FailedState_DoesNotListenToTransportChanges()
        {
            // Arrange
            ConnectionFailedState state = new ConnectionFailedState(null, ErrorInfo.ReasonNeverConnected);

            // Act
            state.OnTransportStateChanged(null);
        }

        [Fact]
        public void FailedState_UpdatesConnectionInformation()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            var target = new Mock<Connection>();
            target.SetupProperty(c => c.Key, "test test");
            context.SetupGet(c => c.Connection).Returns(target.Object);
            ConnectionFailedState state = new ConnectionFailedState(context.Object, ErrorInfo.ReasonNeverConnected);

            // Act
            state.OnAttachedToContext();

            // Assert
            target.VerifySet(c => c.Key = null);
        }
    }
}
