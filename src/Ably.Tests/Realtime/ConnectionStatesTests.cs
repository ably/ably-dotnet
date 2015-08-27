using Ably.Transport;
using Ably.Transport.States.Connection;
using Ably.Types;
using Moq;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class ConnectionStatesTests
    {
        //
        // Initialized state
        //
        #region Initialized
        [Fact]
        public void InitializedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionInitializedState state = new ConnectionInitializedState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Initialized, state.State);
        }

        [Fact]
        public void InitializedState_QueuesMessages()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.QueuedMessages).Returns(new Queue<ProtocolMessage>());
            ConnectionInitializedState state = new ConnectionInitializedState(context.Object);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Connect));

            // Assert
            Assert.Equal<int>(1, context.Object.QueuedMessages.Count);
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
        public void InitializedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionInitializedState state = new ConnectionInitializedState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InitializedState_DoesNotListenToTransportChanges()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionInitializedState state = new ConnectionInitializedState(context.Object);

            // Act
            state.OnTransportStateChanged(null);
        }

        [Fact]
        public void InitializedState_Close_DoesNothing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionInitializedState state = new ConnectionInitializedState(context.Object);

            // Act
            state.Close();
        }

        [Fact]
        public void InitializedState_Connect_GoesToConnecting()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionInitializedState state = new ConnectionInitializedState(context.Object);

            // Act
            state.Connect();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }
        #endregion

        //
        // Connecting state
        //
        #region Connecting
        [Fact]
        public void ConnectingState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Connecting, state.State);
        }

        [Fact]
        public void ConnectingState_QueuesMessages()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.QueuedMessages).Returns(new Queue<ProtocolMessage>());
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Connect));

            // Assert
            Assert.Equal<int>(1, context.Object.QueuedMessages.Count);
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
        [InlineData(ProtocolMessage.MessageAction.Disconnected)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void ConnectingState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ConnectingState_HandlesInboundConnectedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connecting);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConnectingState_HandlesInboundConnectedMessage_DoesNotGoToConnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Closing);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        [Fact]
        public void ConnectingState_HandlesInboundConnectedMessage_GoesToConnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connected);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectedState>()), Times.Once());
        }

        [Fact]
        public void ConnectingState_HandlesInboundErrorMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connected);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConnectingState_HandlesInboundErrorMessage_GoesToFailed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connected);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionFailedState>(ss => object.ReferenceEquals(ss.Error, targetError))), Times.Once());
        }

        [Fact]
        public void ConnectingState_Connect_DoesNothing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.Connect();
        }

        [Fact]
        public void ConnectingState_Close_GoesToClosing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.Close();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosingState>()), Times.Once());
        }

        [Fact]
        public void ConnectingState_AttachToContext_CreatesConnectsTransport()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.CreateTransport()).Callback(() =>
                context.Setup(c => c.Transport).Returns(transport.Object));
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.CreateTransport(), Times.Once());
            transport.Verify(c => c.Connect(), Times.Once());
        }

        [Theory]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connected)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public void ConnectingState_TransportStateChanges_DoesNotSwitchState(TransportState transportState)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(transportState));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        [Fact]
        public void ConnectingState_TransportGoesDisconnected_SwitchesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionDisconnectedState>()), Times.Once());
        }
        #endregion

        //
        // Connected state
        //
        #region Connected
        [Fact]
        public void ConnectedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Connected, state.State);
        }

        [Fact]
        public void ConnectedState_SendsMessages()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));

            // Assert
            transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.Action == ProtocolMessage.MessageAction.Attach)), Times.Once());
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
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void ConnectedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ConnectedState_HandlesInboundDisconnectedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConnectingState_HandlesInboundDisconnectedMessage_GoesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionDisconnectedState>()), Times.Once());
        }

        [Fact]
        public void ConnectedState_HandlesInboundErrorMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConnectedState_HandlesInboundErrorMessage_GoesToFailed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionFailedState>(ss => object.ReferenceEquals(ss.Error, targetError))), Times.Once());
        }

        [Fact]
        public void ConnectedState_Connect_DoesNothing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.Connect();
        }

        [Fact]
        public void ConnectedState_Close_GoesToClosing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.Close();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosingState>()), Times.Once());
        }

        [Fact]
        public void ConnectedState_Close_SendsCloseMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.Close();

            // Assert
            transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.Action == ProtocolMessage.MessageAction.Close)), Times.Once());
        }

        [Fact]
        public void ConnectedState_AttachToContext_SendsPendingMessages()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            var pendingMessages = new Queue<ProtocolMessage>();
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Attach);
            pendingMessages.Enqueue(targetMessage);
            context.Setup(c => c.QueuedMessages).Returns(pendingMessages);
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.OnAttachedToContext();

            // Assert
            transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => object.ReferenceEquals(ss, targetMessage))), Times.Once());
            Assert.Equal<int>(0, pendingMessages.Count);
        }

        [Theory]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connected)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public void ConnectedState_TransportStateChanges_DoesNotSwitchState(TransportState transportState)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(transportState));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        [Fact]
        public void ConnectedState_TransportGoesDisconnected_SwitchesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, null);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionDisconnectedState>()), Times.Once());
        }
        #endregion
    }
}
