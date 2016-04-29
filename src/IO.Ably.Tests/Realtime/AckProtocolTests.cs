using States = IO.Ably.Transport.States.Connection;
using System;
using System.Collections.Generic;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Extensions;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Moq;

namespace IO.Ably.Tests
{
    public class AckProtocolTests
    {
        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        public void WhenSendingMessage_IncrementsMsgSerial(ProtocolMessage.MessageAction messageAction)
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            ProtocolMessage targetMessage1 = new ProtocolMessage(messageAction, "Test");
            ProtocolMessage targetMessage2 = new ProtocolMessage(messageAction, "Test");
            ProtocolMessage targetMessage3 = new ProtocolMessage(messageAction, "Test");

            // Act
            target.SendMessage(targetMessage1, null);
            target.SendMessage(targetMessage2, null);
            target.SendMessage(targetMessage3, null);

            // Assert
            Assert.Equal(0, targetMessage1.msgSerial);
            Assert.Equal(1, targetMessage2.msgSerial);
            Assert.Equal(2, targetMessage3.msgSerial);
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
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void WhenSendingMessage_MsgSerialNotIncremented(ProtocolMessage.MessageAction messageAction)
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            ProtocolMessage targetMessage1 = new ProtocolMessage(messageAction, "Test");
            ProtocolMessage targetMessage2 = new ProtocolMessage(messageAction, "Test");
            ProtocolMessage targetMessage3 = new ProtocolMessage(messageAction, "Test");

            // Act
            target.SendMessage(targetMessage1, null);
            target.SendMessage(targetMessage2, null);
            target.SendMessage(targetMessage3, null);

            // Assert
            Assert.Equal(0, targetMessage1.msgSerial);
            Assert.Equal(0, targetMessage2.msgSerial);
            Assert.Equal(0, targetMessage3.msgSerial);
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        public void WhenReceiveMessage_HandleAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();

            // Act
            bool result = target.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.True(result);
        }

        [Theory]
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
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public void WhenReceiveMessage_DoesNotHandleAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();

            // Act
            bool result = target.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void WhenSendingMessage_AckCallbackCalled()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            long msgSerial = 0;

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = msgSerial++, count = 1 });

            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = msgSerial++, count = 1 });

            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = msgSerial++, count = 1 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
            Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
        }

        [Fact]
        public void WhenSendingMessage_AckCallbackCalled_ForMultipleMessages()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { msgSerial = 0, count = 3 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
            Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            long msgSerial = 0;

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1 });

            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1 });

            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled_ForMultipleMessages()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = 0, count = 3 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled_WithError()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            ErrorInfo error = new ErrorInfo("reason", 123);
            long msgSerial = 0;

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1, error = error });

            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1, error = error });

            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = msgSerial++, count = 1, error = error });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
        }

        [Fact]
        public void WhenSendingMessage_NackCallbackCalled_ForMultipleMessages_WithError()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            ErrorInfo error = new ErrorInfo("reason", 123);

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { msgSerial = 0, count = 3, error = error });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
        }

        [Fact]
        public void OnState_Connected_MsgSerialReset()
        {
            // Arrange
            Mock<IConnectionContext> context = new Mock<IConnectionContext>();
            context.SetupGet(c => c.Connection).Returns(new Connection(new Mock<IConnectionManager>().Object));

            AcknowledgementProcessor target = new AcknowledgementProcessor();
            ProtocolMessage targetMessage1 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
            ProtocolMessage targetMessage2 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
            ProtocolMessage targetMessage3 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
            ProtocolMessage targetMessage4 = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");

            // Act
            target.SendMessage(targetMessage1, null);
            target.SendMessage(targetMessage2, null);
            target.OnStateChanged(new ConnectionConnectedState(context.Object, new ConnectionInfo("", 0, "", "")));
            target.SendMessage(targetMessage3, null);
            target.SendMessage(targetMessage4, null);

            // Assert
            Assert.Equal(0, targetMessage1.msgSerial);
            Assert.Equal(1, targetMessage2.msgSerial);
            Assert.Equal(0, targetMessage3.msgSerial);
            Assert.Equal(1, targetMessage4.msgSerial);
        }

        [Fact]
        public void OnState_Closed_FailCallbackCalled()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            ErrorInfo error = new ErrorInfo("reason", 123);

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnStateChanged(new ConnectionClosedState(null, error));

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
        }

        [Fact]
        public void OnState_Failed_FailCallbackCalled()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor();
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            ErrorInfo error = new ErrorInfo("reason", 123);

            // Act
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnStateChanged(new ConnectionFailedState(null, error));

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => object.ReferenceEquals(c.Item2, error))); // Error
        }
    }
}
