using System;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class SuspendedStateSpecs : AblySpecs
    {
        [Fact]
        public void SuspendedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionSuspendedState state = new ConnectionSuspendedState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionStateType>(Ably.Realtime.ConnectionStateType.Suspended, state.State);
        }

        [Fact]
        public void SuspendedState_SendMessage_DoesNothing()
        {
            // Arrange
            ConnectionSuspendedState state = new ConnectionSuspendedState(null);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));
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
        public async Task SuspendedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionSuspendedState state = new ConnectionSuspendedState(null);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SuspendedState_DoesNotListenToTransportChanges()
        {
            // Arrange
            ConnectionSuspendedState state = new ConnectionSuspendedState(null);

            // Act
            state.OnTransportStateChanged(null);
        }

        [Fact]
        public void SuspendedState_Close_GoesToClosed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionSuspendedState state = new ConnectionSuspendedState(context.Object);

            // Act
            state.Close();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public void SuspendedState_Connect_GoesToConnecting()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionSuspendedState state = new ConnectionSuspendedState(context.Object);

            // Act
            state.Connect();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }

        [Fact]
        public void SuspendedState_RetriesConnection()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Initialized);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            timer.Setup(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<System.Action>(), false)).Callback<int, System.Action>((t, c) => c());
            ConnectionSuspendedState state = new ConnectionSuspendedState(context.Object, null, timer.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<System.Action>(), false), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }

        public SuspendedStateSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}