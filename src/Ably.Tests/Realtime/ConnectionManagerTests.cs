using Ably.Realtime;
using Ably.Transport;
using States = Ably.Transport.States.Connection;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;
using Ably.Types;

namespace Ably.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void When_Created_StateIsInitialized()
        {
            // Arrange
            IConnectionContext target = new ConnectionManager(new AblyRealtimeOptions());

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Initialized, target.State.State);
        }

        #region StateCommunication

        [Fact]
        public void WhenTransportConnected_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closing);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            transport.Object.Listener.OnTransportConnected();

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<States.ConnectionState.TransportStateInfo>(ss => ss.State == TransportState.Connected)), Times.Once());
        }

        [Fact]
        public void WhenTransportDisconnected_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closing);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            transport.Object.Listener.OnTransportDisconnected();

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<States.ConnectionState.TransportStateInfo>(ss =>
                ss.State == TransportState.Closed)), Times.Once());
        }

        [Fact]
        public void WhenTransportError_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            Exception targetError = new Exception("test");

            // Act
            transport.Object.Listener.OnTransportError(targetError);

            // Assert
            state.Verify(c => c.OnTransportStateChanged(It.Is<States.ConnectionState.TransportStateInfo>(ss =>
                ss.Error == targetError && ss.State == TransportState.Closed)), Times.Once());
        }

        [Fact]
        public void WhenTransportMessageReceived_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            state.Verify(c => c.OnMessageReceived(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenTransportMessageReceived_StateHandlesIt_NoMessageReceived()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            state.Setup(c => c.OnMessageReceived(It.IsAny<ProtocolMessage>())).Returns(true);
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            bool eventCalled = false;
            target.MessageReceived += (m) => eventCalled = true;
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Ack);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            Assert.False(eventCalled);
        }

        [Fact]
        public void WhenTransportMessageReceived_StateNotHandlesIt_MessageReceived()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            state.Setup(c => c.OnMessageReceived(It.IsAny<ProtocolMessage>())).Returns(false);
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            bool eventCalled = false;
            target.MessageReceived += (m) => eventCalled = true;
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            Assert.True(eventCalled);
        }

        [Fact]
        public void WhenConnect_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            target.Connect();

            // Assert
            state.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void WhenClose_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);

            // Act
            target.Close();

            // Assert
            state.Verify(c => c.Close(), Times.Once());
        }

        [Fact]
        public void WhenSend_StateCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            target.Send(targetMessage, null);

            // Assert
            state.Verify(c => c.SendMessage(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenSetState_OnAttachedToContextCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            IConnectionContext target = new ConnectionManager(transport.Object, ackProcessor.Object, null);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            target.SetState(state.Object);

            // Assert
            state.Verify(c => c.OnAttachedToContext(), Times.Once());
        }

        #endregion

        #region AckProcessorCommunication

        [Fact]
        public void WhenTransportMessageReceived_AckCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            ackProcessor.Verify(c => c.OnMessageReceived(targetMessage), Times.Once());
        }

        [Fact]
        public void WhenTransportMessageReceived_AckHandlesIt_NoMessageReceived()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ackProcessor.Setup(c => c.OnMessageReceived(It.IsAny<ProtocolMessage>())).Returns(true);
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            bool eventCalled = false;
            target.MessageReceived += (m) => eventCalled = true;
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Ack);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            Assert.False(eventCalled);
        }

        [Fact]
        public void WhenTransportMessageReceived_AckNotHandlesIt_MessageReceived()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupGet(c => c.State).Returns(TransportState.Closed);
            transport.SetupProperty(c => c.Listener);
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ackProcessor.Setup(c => c.OnMessageReceived(It.IsAny<ProtocolMessage>())).Returns(false);
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            bool eventCalled = false;
            target.MessageReceived += (m) => eventCalled = true;
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            transport.Object.Listener.OnTransportMessageReceived(targetMessage);

            // Assert
            Assert.True(eventCalled);
        }

        [Fact]
        public void WhenSetState_AckCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            IConnectionContext target = new ConnectionManager(transport.Object, ackProcessor.Object, null);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);

            // Act
            target.SetState(state.Object);

            // Assert
            ackProcessor.Verify(c => c.OnStateChanged(state.Object), Times.Once());
        }

        [Fact]
        public void WhenSend_AckCallbackCalled()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);
            Action<bool, ErrorInfo> targetAction = (b, e) => { };

            // Act
            target.Send(targetMessage, targetAction);

            // Assert
            ackProcessor.Verify(c => c.SendMessage(targetMessage, targetAction), Times.Once());
        }

        [Fact]
        public void WhenSend_AckCalledBeforeState()
        {
            // Arrange
            Mock<States.ConnectionState> state = new Mock<States.ConnectionState>();
            Mock<ITransport> transport = new Mock<ITransport>();
            Mock<IAcknowledgementProcessor> ackProcessor = new Mock<IAcknowledgementProcessor>();
            string firstCall = null;
            ackProcessor.Setup(c => c.SendMessage(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()))
                .Callback(() => { if (firstCall == null) firstCall = "ack"; });
            state.Setup(c => c.SendMessage(It.IsAny<ProtocolMessage>()))
                .Callback(() => { if (firstCall == null) firstCall = "state"; });
            ConnectionManager target = new ConnectionManager(transport.Object, ackProcessor.Object, state.Object);
            ProtocolMessage targetMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message);
            Action<bool, ErrorInfo> targetAction = (b, e) => { };

            // Act
            target.Send(targetMessage, targetAction);

            // Assert
            Assert.Equal<String>("ack", firstCall);
        }

        #endregion

        [Fact]
        public void When_HostSetInOptions_CreateTransportParameters_DoesNotModifyIt()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Host = "http://test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options);

            // Assert
            Assert.Equal<string>(options.Host, target.Host);
        }

        [Theory]
        [InlineData(AblyEnvironment.Sandbox)]
        [InlineData(AblyEnvironment.Uat)]
        public void When_EnvironmentSetInOptions_CreateCorrectTransportParameters(AblyEnvironment environment)
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Environment = environment;
            options.Host = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options);

            // Assert
            Assert.Equal<string>(string.Format("{0}-{1}", environment, options.Host).ToLower(), target.Host);
        }

        [Fact]
        public void When_EnvironmentSetInOptions_Live_CreateCorrectTransportParameters()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();
            options.Environment = AblyEnvironment.Live;
            options.Host = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options);

            // Assert
            Assert.Equal<string>(options.Host, target.Host);
        }
    }
}
