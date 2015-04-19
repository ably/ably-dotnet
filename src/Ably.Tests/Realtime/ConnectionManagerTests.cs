using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

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
        public void Close_When_Initialized_DoesNotSendDisconnect()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Initialized);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Close();

            // Assert
            mock.Verify(c => c.Close(true), Times.Never());
            mock.Verify(c => c.Close(false), Times.Once());
        }

        [Fact]
        public void Close_When_Connected_SendsDisconnect()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Close();

            // Assert
            mock.Verify(c => c.Close(true), Times.Once());
            mock.Verify(c => c.Close(false), Times.Never());
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
            mock.SetupGet(c => c.State).Returns(TransportState.Connecting);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, "Test"), null);
            target.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat), null);
            mock.Object.Listener.OnTransportConnected();

            // Assert
            mock.Verify(c => c.Send(It.IsAny<ProtocolMessage>()), Times.Exactly(2));
        }

        [Fact]
        public void When_TransportError_CallsStateChanged_Failed()
        {
            // Arrange
            Mock<ITransport> transport = new Mock<ITransport>();
            transport.SetupProperty(c => c.Listener);
            ConnectionManager manager = new ConnectionManager(transport.Object);
            List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>> args = new List<Tuple<ConnectionState, ConnectionInfo, ErrorInfo>>();
            manager.StateChanged += (s, i, e) =>
            {
                args.Add(new Tuple<ConnectionState, ConnectionInfo, ErrorInfo>(s, i, e));
            };
            Exception targetException = new Exception("reason");

            // Act
            transport.Object.Listener.OnTransportError(targetException);

            // Assert
            Assert.Single(args, t => t.Item1 == ConnectionState.Failed && t.Item2 == null && t.Item3 != null &&
                t.Item3.Reason == targetException.Message && t.Item3.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);
        }
    }
}
