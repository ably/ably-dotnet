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
            ConnectionInitializedState state = new ConnectionInitializedState(null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InitializedState_DoesNotListenToTransportChanges()
        {
            // Arrange
            ConnectionInitializedState state = new ConnectionInitializedState(null);

            // Act
            state.OnTransportStateChanged(null);
        }

        [Fact]
        public void InitializedState_Close_DoesNothing()
        {
            // Arrange
            ConnectionInitializedState state = new ConnectionInitializedState(null);

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
            ConnectionConnectingState state = new ConnectionConnectingState(null);

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
            ConnectionConnectingState state = new ConnectionConnectingState(null);

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
            ConnectionConnectedState state = new ConnectionConnectedState(null, null);

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
            ConnectionConnectedState state = new ConnectionConnectedState(null, null);

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

        //
        // Disconnected state
        //
        #region Disconnected
        [Fact]
        public void DisconnectedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(context.Object, ErrorInfo.ReasonClosed);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Disconnected, state.State);
        }
        #endregion

        //
        // Suspended state
        //
        #region Suspended
        [Fact]
        public void SuspendedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionSuspendedState state = new ConnectionSuspendedState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Suspended, state.State);
        }
        #endregion

        //
        // Closing state
        //
        #region Closing
        [Fact]
        public void ClosingState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Closing, state.State);
        }

        [Fact]
        public void ClosingState_SendMessage_DoesNothing()
        {
            // Arrange
            ConnectionClosingState state = new ConnectionClosingState(null);

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));
        }
        #endregion

        //
        // Closed state
        //
        #region Closed
        [Fact]
        public void ClosedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosedState state = new ConnectionClosedState(context.Object);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Closed, state.State);
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
        public void ClosedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionClosedState state = new ConnectionClosedState(null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

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
        #endregion

        //
        // Failed state
        //
        #region Failed
        [Fact]
        public void FailedState_CorrectState()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionFailedState state = new ConnectionFailedState(context.Object, ErrorInfo.ReasonNeverConnected);

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Failed, state.State);
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
        public void FailedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionFailedState state = new ConnectionFailedState(null, ErrorInfo.ReasonNeverConnected);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

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
        #endregion
    }
}
