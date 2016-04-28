﻿using Moq;
using System;
using System.Collections.Generic;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Extensions;

namespace IO.Ably.Tests
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
        public void ConnectingState_AttemptsConnection()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Transport).Returns(new Mock<ITransport>().Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.AttemptConnection(), Times.Once());
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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
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
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionFailedState>(ss => object.ReferenceEquals(ss.Error, targetError))), Times.Once());
        }

        [Theory]
        [InlineData(System.Net.HttpStatusCode.InternalServerError)]
        [InlineData(System.Net.HttpStatusCode.GatewayTimeout)]
        public void ConnectingState_HandlesInboundErrorMessage_GoesToDisconnected(System.Net.HttpStatusCode code)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.Setup(c => c.Connection).Returns(new Realtime.Connection());
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connected);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);
            ErrorInfo targetError = new ErrorInfo("test", 123, code);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionDisconnectedState>(ss => ss.UseFallbackHost == true)), Times.Once());
        }

        [Fact]
        public void ConnectingState_HandlesInboundErrorMessage_ClearsConnectionKey()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<Realtime.Connection> connection = new Mock<Realtime.Connection>();
            connection.SetupProperty(c => c.Key);
            context.Setup(c => c.Connection).Returns(connection.Object);
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.Setup(c => c.State).Returns(TransportState.Connected);
            context.Setup(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = new ErrorInfo("test", 123, System.Net.HttpStatusCode.InternalServerError) });

            // Assert
            connection.VerifySet(c => c.Key = null);
        }

        [Fact]
        public void ConnectingState_HandlesInboundDisconnectedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

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
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionDisconnectedState>(ss => ss.UseFallbackHost == false)), Times.Once());
        }

        [Theory]
        [InlineData(System.Net.HttpStatusCode.InternalServerError)]
        [InlineData(System.Net.HttpStatusCode.GatewayTimeout)]
        public void ConnectingState_HandlesInboundDisconnectedMessage_GoesToDisconnected_FallbackHost(System.Net.HttpStatusCode code)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected) { error = new ErrorInfo("", 0, code) });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionDisconnectedState>(ss => ss.UseFallbackHost == true)), Times.Once());
        }

        [Fact]
        public void ConnectingState_HandlesInboundDisconnectedMessage_GoesToSuspended()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.FirstConnectionAttempt).Returns(DateTimeOffset.UtcNow.AddHours(-3));
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionSuspendedState>()), Times.Once());
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
        public void ConnectingState_AttachToContext_NoTransport_CreatesTransport()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.Setup(c => c.CreateTransport(It.IsAny<bool>()))
                .Callback(() => context.Setup(c => c.Transport).Returns(new Mock<ITransport>().Object));
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.CreateTransport(false), Times.Once());
        }

        [Fact]
        public void ConnectingState_AttachToContext_Fallback_CreatesTransport()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.Setup(c => c.CreateTransport(It.IsAny<bool>()))
                .Callback(() => context.Setup(c => c.Transport).Returns(new Mock<ITransport>().Object));
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object, true);

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.CreateTransport(true), Times.Once());
        }

        [Fact]
        public void ConnectingState_AttachToContext_ClosedTransport_Connects()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            transport.Verify(c => c.Connect(), Times.Once());
        }

        //[Fact]
        //public void ConnectingState_AttachToContext_ConnectedTransport_SendsConnect()
        //{
        //    // Arrange
        //    Mock<IConnectionContext> context = new Mock<IConnectionContext>();
        //    Mock<ITransport> transport = new Mock<ITransport>();
        //    context.SetupGet(c => c.Transport).Returns(transport.Object);
        //    transport.SetupGet(c => c.State).Returns(TransportState.Connected);
        //    ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

        //    // Act
        //    state.OnAttachedToContext();

        //    // Assert
        //    transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.Action == ProtocolMessage.MessageAction.Connect)), Times.Once());
        //}

        [Theory]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connected)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public void ConnectingState_TransportStateChanges_DoesNotSwitchState(TransportState transportState)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(transportState));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        //[Fact]
        //public void ConnectingState_TransportGoesConnected_SendsConnect()
        //{
        //    // Arrange
        //    Mock<IConnectionContext> context = new Mock<IConnectionContext>();
        //    Mock<ITransport> transport = new Mock<ITransport>();
        //    context.SetupGet(c => c.Transport).Returns(transport.Object);
        //    ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

        //    // Act
        //    state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Connected));

        //    // Assert
        //    transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.Action == ProtocolMessage.MessageAction.Connect)), Times.Once());
        //}

        [Fact]
        public void ConnectingState_TransportGoesDisconnected_SwitchesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionDisconnectedState>(ss => ss.UseFallbackHost == false)), Times.Once());
        }

        [Fact]
        public void ConnectingState_TransportGoesDisconnected_SwitchesToDisconnected_WithError()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed, new Exception()));

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionDisconnectedState>(ss => ss.UseFallbackHost == true)), Times.Once());
        }

        [Fact]
        public void ConnectingState_TransportGoesDisconnected_SwitchesToSuspended()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.FirstConnectionAttempt).Returns(DateTimeOffset.UtcNow.AddHours(-3));
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object);

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionSuspendedState>()), Times.Once());
        }

        [Fact]
        public void ConnectingState_ForceDisconnect()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Initialized);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            timer.Setup(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>())).Callback<int, System.Action>((t, c) => c());
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object, timer.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            context.Verify(c => c.SetState(It.Is<ConnectionDisconnectedState>(ss => ss.Error == ErrorInfo.ReasonTimeout)), Times.Once());
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Disconnected)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        public void ConnectingState_ForceDisconnectNotApplied_WhenMessageReceived(ProtocolMessage.MessageAction action)
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Initialized);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object, timer.Object);

            // Act
            state.OnAttachedToContext();
            transport.SetupGet(c => c.State).Returns(TransportState.Connected);
            state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            timer.Verify(c => c.Abort(), Times.Once);
        }

        [Fact]
        public void ConnectingState_ForceDisconnectNotApplied_WhenTransportClosed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Initialized);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            ConnectionConnectingState state = new ConnectionConnectingState(context.Object, timer.Object);

            // Act
            state.OnAttachedToContext();
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            timer.Verify(c => c.Abort(), Times.Once);
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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Assert
            Assert.Equal<Ably.Realtime.ConnectionState>(Ably.Realtime.ConnectionState.Connected, state.State);
        }

        [Fact]
        public void ConnectedState_ResetsConnectionAttempts()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            state.OnAttachedToContext();

            // Assert
            context.Verify(c => c.ResetConnectionAttempts(), Times.Once());
        }

        [Fact]
        public void ConnectedState_SendsMessages()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.Transport).Returns(transport.Object);
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));

            // Assert
            transport.Verify(c => c.Send(It.Is<ProtocolMessage>(ss => ss.action == ProtocolMessage.MessageAction.Attach)), Times.Once());
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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        [Fact]
        public void ConnectedState_HandlesInboundDisconnectedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConnectedState_HandlesInboundDisconnectedMessage_GoesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionFailedState>(ss => object.ReferenceEquals(ss.Error, targetError))), Times.Once());
        }

        [Fact]
        public void ConnectedState_Connect_DoesNothing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            state.Connect();

            // Asser
            context.Verify(c => c.SetState(It.IsAny<ConnectionState>()), Times.Never());
        }

        [Fact]
        public void ConnectedState_Close_GoesToClosing()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.Transport).Returns(transport.Object);
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            state.Close();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosingState>()), Times.Once());
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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, ""));

            // Act
            state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionDisconnectedState>()), Times.Once());
        }

        [Fact]
        public void ConnectedState_UpdatesConnectionInformation()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<Realtime.Connection> target = new Mock<Realtime.Connection>();
            context.SetupGet(c => c.Connection).Returns(target.Object);

            // Act
            ConnectionConnectedState state = new ConnectionConnectedState(context.Object, new ConnectionInfo("test", 12564, "test test"));

            // Assert
            target.VerifySet(c => c.Id = "test");
            target.VerifySet(c => c.Serial = 12564);
            target.VerifySet(c => c.Key = "test test");
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

        [Fact]
        public void DisconnectedState_QueuesMessages()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.QueuedMessages).Returns(new Queue<ProtocolMessage>());
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(context.Object, ErrorInfo.ReasonClosed);

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
        public void DisconnectedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(null, ErrorInfo.ReasonClosed);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DisconnectedState_DoesNotListenToTransportChanges()
        {
            // Arrange
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(null, ErrorInfo.ReasonClosed);

            // Act
            state.OnTransportStateChanged(null);
        }

        [Fact]
        public void DisconnectedState_Close_GoesToClosed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(context.Object, ErrorInfo.ReasonClosed);

            // Act
            state.Close();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public void DisconnectedState_Connect_GoesToConnecting()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(context.Object, ErrorInfo.ReasonClosed);

            // Act
            state.Connect();

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }

        [Fact]
        public void DisconnectedState_RetriesConnection()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Initialized);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            timer.Setup(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>())).Callback<int, System.Action>((t, c) => c());
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(context.Object, ErrorInfo.ReasonClosed, timer.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
        }

        [Fact]
        public void DisconnectedState_Fallback_RetriesConnectionImmediately()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Initialized);
            context.SetupGet(c => c.Transport).Returns(transport.Object);
            Mock<ICountdownTimer> timer = new Mock<ICountdownTimer>();
            ConnectionDisconnectedState state = new ConnectionDisconnectedState(context.Object, ErrorInfo.ReasonClosed, timer.Object);
            state.UseFallbackHost = true;

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Never());
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
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
        public void SuspendedState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionSuspendedState state = new ConnectionSuspendedState(null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

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
            timer.Setup(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>())).Callback<int, System.Action>((t, c) => c());
            ConnectionSuspendedState state = new ConnectionSuspendedState(context.Object, null, timer.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionConnectingState>()), Times.Once());
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
        public void ClosingState_DoesNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            ConnectionClosingState state = new ConnectionClosingState(null);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ClosingState_HandlesInboundClosedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ClosingState_HandlesInboundClosedMessage_GoesToClosed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public void ClosingState_HandlesInboundErrorMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ClosingState_HandlesInboundErrorMessage_GoesToFailed()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);
            ErrorInfo targetError = new ErrorInfo("test", 123);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            context.Verify(c => c.SetState(It.Is<ConnectionFailedState>(ss => object.ReferenceEquals(ss.Error, targetError))), Times.Once());
        }

        [Fact]
        public void ClosingState_HandlesInboundDisconnectedMessage()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            bool result = state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ClosingState_HandlesInboundDisconnectedMessage_GoesToDisconnected()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            ConnectionClosingState state = new ConnectionClosingState(context.Object);

            // Act
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

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
            timer.Setup(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>())).Callback<int, System.Action>((t, c) => c());
            ConnectionClosingState state = new ConnectionClosingState(context.Object, null, timer.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public void ClosingState_ForceCloseNotApplied_WhenClosedMessageReceived()
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
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            timer.Verify(c => c.Abort(), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionClosedState>()), Times.Once());
        }

        [Fact]
        public void ClosingState_ForceCloseNotApplied_WhenErrorMessageReceived()
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
            state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            timer.Verify(c => c.Start(It.IsAny<int>(), It.IsAny<System.Action>()), Times.Once);
            timer.Verify(c => c.Abort(), Times.Once);
            context.Verify(c => c.SetState(It.IsAny<ConnectionFailedState>()), Times.Once());
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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.CreateTransport(It.IsAny<bool>())).Callback(() =>
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

        [Fact]
        public void ClosedState_UpdatesConnectionInformation()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<Realtime.Connection> target = new Mock<Realtime.Connection>();
            target.SetupProperty(c => c.Key, "test test");
            context.SetupGet(c => c.Connection).Returns(target.Object);
            ConnectionClosedState state = new ConnectionClosedState(context.Object);

            // Act
            state.OnAttachedToContext();

            // Assert
            target.VerifySet(c => c.Key = null);
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
            context.SetupGet(c => c.Connection).Returns(new Realtime.Connection(new Mock<IConnectionManager>().Object));
            Mock<ITransport> transport = new Mock<ITransport>();
            context.Setup(c => c.CreateTransport(It.IsAny<bool>())).Callback(() =>
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

        [Fact]
        public void FailedState_UpdatesConnectionInformation()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            Mock<Realtime.Connection> target = new Mock<Realtime.Connection>();
            target.SetupProperty(c => c.Key, "test test");
            context.SetupGet(c => c.Connection).Returns(target.Object);
            ConnectionFailedState state = new ConnectionFailedState(context.Object, ErrorInfo.ReasonNeverConnected);

            // Act
            state.OnAttachedToContext();

            // Assert
            target.VerifySet(c => c.Key = null);
        }
        #endregion

        [Fact]
        public void CountdownTimer_Start_StartsCountdown()
        {
            // Arrange
            CountdownTimer timer = new CountdownTimer();
            int timeout = 10;
            int called = 0;
            System.Action callback = () => called++;

            // Act
            timer.Start(timeout, callback);
            System.Threading.Thread.Sleep(50);

            // Assert
            Assert.Equal<int>(1, called);
        }

        [Fact]
        public void CountdownTimer_Abort_StopsCountdown()
        {
            // Arrange
            CountdownTimer timer = new CountdownTimer();
            int timeout = 10;
            int called = 0;
            System.Action callback = () => called++;
            timer.Start(timeout, callback);

            // Act
            timer.Abort();
            System.Threading.Thread.Sleep(50);

            // Assert
            Assert.Equal<int>(0, called);
        }

        [Fact(Skip = "Inconsistent test")]
        public void CountdownTimer_AbortStart_StartsNewCountdown()
        {
            // Arrange
            CountdownTimer timer = new CountdownTimer();
            int timeout = 10;
            int called = 0;
            Action callback = () => called++;
            timer.Start(timeout, callback);

            // Act
            timer.Abort();
            timer.Start(timeout, callback);
            System.Threading.Thread.Sleep(50);

            // Assert
            Assert.Equal<int>(1, called);
        }

        //TODO: MG Fix the inconsistent test
        [Fact(Skip = "Inconsistent test. It has concurrency issues.")]
        public void CountdownTimer_StartTwice_AbortsOldTimer()
        {
            // Arrange
            CountdownTimer timer = new CountdownTimer();
            int timeout = 10;
            int called = 0;
            Action callback = () => called++;

            // Act
            timer.Start(timeout, callback);
            timer.Start(timeout, callback);
            System.Threading.Thread.Sleep(50);

            // Assert
            Assert.Equal<int>(1, called);
        }
    }
}
