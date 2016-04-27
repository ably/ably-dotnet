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
    public class ClosingStateSpecs : AblySpecs
    {
        [Fact]
        public void ClosingState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionStateType>(Ably.Realtime.ConnectionStateType.Closing, state.State);
        }

        [Fact]
        public void ClosingState_SendMessage_DoesNothing()
        {
            // Arrange
            ConnectionClosingState state = new ConnectionClosingState(null);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));
        }

        [Fact]
        public void ClosingState_Connect_DoesNothing()
        {
            // Arrange
            ConnectionClosingState state = new ConnectionClosingState(null);

            // Act
            state.Connect();
        }

        [Fact]
        public void ClosingState_Close_DoesNothing()
        {
            // Arrange
            ConnectionClosingState state = new ConnectionClosingState(null);

            // Act
            state.Close();
        }

        [Fact]
        public void ClosingState_TransportGoesDisconnected_SwitchesToClosed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Theory]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connected)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public void ClosingState_TransportStateChanges_DoesNotSwitchState(TransportState transportState)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(transportState));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Attached)]
        [InlineData(ProtocolMessage.MessageAction.Close)]
        [InlineData(ProtocolMessage.MessageAction.Connect)]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Detached)]
        [InlineData(ProtocolMessage.MessageAction.Disconnect)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public async Task ClosingState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionClosingState state = new ConnectionClosingState(null);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ClosingState_HandlesInboundClosedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ClosingState_HandlesInboundClosedMessage_GoesToClosed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public async Task ClosingState_HandlesInboundErrorMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ClosingState_HandlesInboundErrorMessage_GoesToFailed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionFailedState>(ss => object.ReferenceEquals(ss.Error, targetError))), Times.Once());
        }

        [Fact]
        public async Task ClosingState_HandlesInboundDisconnectedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            bool result = await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ClosingState_HandlesInboundDisconnectedMessage_GoesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionDisconnectedState>()), Times.Once());
        }

        [Fact]
        public void ClosingState_AttachToContext_ConnectedTransport_SendsClose()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Connected);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.action == ProtocolMessage.MessageAction.Close)), Times.Once());
        }

        [Theory]
        [InlineData(TransportState.Closed)]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public void ClosingState_AttachToContext_TransportNotConnected_GoesToClosedState(TransportState transportState)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(transportState);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public void ClosingState_ForceClose()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Connected);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            timer.Setup(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<System.Action>(), false)).Callback<int, System.Action>((t, c) => c());
            ConnectionClosingState state = new ConnectionClosingState(context.Object, null, timer.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<System.Action>(), false), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public async Task ClosingState_ForceCloseNotApplied_WhenClosedMessageReceived()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Connected);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object, null, timer.Object);

            // Act
            state.OnAttachedToContext();
            await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            timer.Verify(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<System.Action>(), false), Times.Once);
            timer.Verify(c => c.Abort(), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public async Task ClosingState_ForceCloseNotApplied_WhenErrorMessageReceived()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Connected);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object, null, timer.Object);

            // Act
            state.OnAttachedToContext();
            await state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            timer.Verify(c => c.Start(It.IsAny<TimeSpan>(), It.IsAny<System.Action>(), false), Times.Once);
            timer.Verify(c => c.Abort(), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionFailedState>()), Times.Once());
        }

        public ClosingStateSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}