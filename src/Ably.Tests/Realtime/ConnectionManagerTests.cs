using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace Ably.Tests
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void When_Initialized_CallsConnect()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Initialized);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Connect();

            // Assert
            mock.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void When_AlreadyConnected_DoesNothing()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Connect();

            // Assert
            mock.Verify(c => c.Connect(), Times.Never());
        }

        [Fact]
        public void WhenConnecting_OutboundMessagesAreNotSent()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Connecting);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, "Test"), null);

            // Assert
            mock.Verify(c => c.Send(It.IsAny<ProtocolMessage>()), Times.Never());
        }

        [Fact]
        public void WhenConnected_OutboundMessagesAreSent()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);
            target.Connect();
            mock.Object.Listener.OnTransportConnected();
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, "Test"), null);
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat), null);

            // Assert
            mock.Verify(c => c.Send(It.IsAny<ProtocolMessage>()), Times.Exactly(2));
        }

        [Fact]
        public void WhenSendingMessage_IncrementsMsgSerial()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            List<ProtocolMessage> sentMessages = new List<ProtocolMessage>();
            mock.Setup(c => c.Send(It.IsAny<ProtocolMessage>())).Callback<ProtocolMessage>(p => sentMessages.Add(p));
            ConnectionManager target = new ConnectionManager(mock.Object);
            target.Connect();
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), null);
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), null);
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), null);

            // Assert
            Assert.Equal(3, sentMessages.Count);
            Assert.Equal(0, sentMessages[0].MsgSerial);
            Assert.Equal(1, sentMessages[1].MsgSerial);
            Assert.Equal(2, sentMessages[2].MsgSerial);
        }

        [Fact]
        public void WhenSendingMessage_AckCallbackCalled()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            long msgSerial = 0;
            mock.Setup(c => c.Send(It.IsAny<ProtocolMessage>())).Callback(() =>
            {
                mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { MsgSerial = msgSerial++, Count = 1 });
            });
            ConnectionManager target = new ConnectionManager(mock.Object);
            target.Connect();
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
            Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
        }

        [Fact]
        public void WhenSendingMessage_AckCallbackCalled_ForMultipleMessages()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);
            target.Connect();
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { MsgSerial = 0, Count = 3 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
            Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            long msgSerial = 0;
            mock.Setup(c => c.Send(It.IsAny<ProtocolMessage>())).Callback(() =>
            {
                mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = msgSerial++, Count = 1 });
            });
            ConnectionManager target = new ConnectionManager(mock.Object);
            target.Connect();
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled_ForMultipleMessages()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = 0, Count = 3 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled_WithError()
        {
            // Arrange
            ErrorInfo error = new ErrorInfo("reason", 123);
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            long msgSerial = 0;
            mock.Setup(c => c.Send(It.IsAny<ProtocolMessage>())).Callback(() =>
            {
                mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = msgSerial++, Count = 1, Error = error });
            });
            ConnectionManager target = new ConnectionManager(mock.Object);
            target.Connect();
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled_ForMultipleMessages_WithError()
        {
            // Arrange
            ErrorInfo error = new ErrorInfo("reason", 123);
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = 0, Count = 3, Error = error });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
        }

        [Fact]
        public void WhenTransportConnected_StateIsNotChanged()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager manager = new ConnectionManager(mock.Object);
            List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>> args = new List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>>();
            manager.Connect();
            manager.StateChanged += (s, i, e) =>
            {
                args.Add(new Tuple<ConnectionState, ConnectionInfo, ErrorInfo>(s, i, e));
            };

            // Act
            mock.Object.Listener.OnTransportConnected();

            // Assert
            Assert.Equal<int>(0, args.Count);
        }

        [Fact]
        public void WhenTransportReceivesConnectedMessage_StateIsConnected()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupProperty(c => c.Listener);
            ConnectionManager manager = new ConnectionManager(mock.Object);
            List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>> args = new List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>>();
            manager.Connect();
            manager.StateChanged += (s, i, e) =>
            {
                args.Add(new Tuple<ConnectionState, ConnectionInfo, ErrorInfo>(s, i, e));
            };

            // Act
            mock.Object.Listener.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            // Assert
            Assert.Equal<int>(1, args.Count);
            Assert.Equal<ConnectionState>(ConnectionState.Connected, args[0].Item1);
        }

        [Fact]
        public void When_Connecting_TransportError_CallsStateChanged_Failed()
        {
            // Arrange
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupProperty(c => c.Listener);
            ConnectionManager manager = new ConnectionManager(transport.Object);
            manager.Connect();
            List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>> args = new List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>>();
            manager.StateChanged += (s, i, e) =>
            {
                args.Add(new Tuple<ConnectionState, ConnectionInfo, ErrorInfo>(s, i, e));
            };
            Exception targetException = new Exception("reason");

            // Act
            transport.Object.Listener.OnTransportError(targetException);

            // Assert
            Assert.Single(args, t => t.Item1 == ConnectionState.Failed && t.Item2 == null && t.Item3 != null);
        }

        [Fact]
        public void When_HostNotSetInOptions_CreateTransportParameters_UsingDefault()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options);

            // Assert
            Assert.Equal<string>("realtime.ably.io", target.Host);
        }

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

        [Fact]
        public void When_Created_StateIsInitialized()
        {
            // Arrange
            IConnectionContext manager = new ConnectionManager();

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Initialized, manager.State.State);
        }
    }
}
