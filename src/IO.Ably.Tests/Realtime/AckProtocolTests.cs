using States = IO.Ably.Transport.States.Connection;
using System;
using System.Collections.Generic;
using IO.Ably.Realtime;
using Xunit;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class AckProtocolTests : AblySpecs
    {
        private Connection _connection;
        private readonly AcknowledgementProcessor _ackProcessor;

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [Trait("spec", "RTN7a")]
        [Trait("spec", "RTN7b")]
        [Trait("sandboxTest", "needed")]
        public void WhenSendingPresenceOrDataMessage_IncrementsMsgSerial(ProtocolMessage.MessageAction messageAction)
        {
            // Arrange
            var targetMessage1 = new ProtocolMessage(messageAction, "Test");
            var targetMessage2 = new ProtocolMessage(messageAction, "Test");
            var targetMessage3 = new ProtocolMessage(messageAction, "Test");

            // Act
            _ackProcessor.QueueIfNecessary(targetMessage1, null);
            _ackProcessor.QueueIfNecessary(targetMessage2, null);
            _ackProcessor.QueueIfNecessary(targetMessage3, null);

            // Assert
            Assert.Equal(0, targetMessage1.MsgSerial);
            Assert.Equal(1, targetMessage2.MsgSerial);
            Assert.Equal(2, targetMessage3.MsgSerial);
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
        [Trait("spec", "RTN7a")]
        public void WhenSendingNotAPresenceOrDataMessage_MsgSerialNotIncremented(ProtocolMessage.MessageAction messageAction)
        {
            // Arrange
            var targetMessage1 = new ProtocolMessage(messageAction, "Test");
            var targetMessage2 = new ProtocolMessage(messageAction, "Test");
            var targetMessage3 = new ProtocolMessage(messageAction, "Test");

            // Act
            _ackProcessor.QueueIfNecessary(targetMessage1, null);
            _ackProcessor.QueueIfNecessary(targetMessage2, null);
            _ackProcessor.QueueIfNecessary(targetMessage3, null);

            // Assert
            Assert.Equal(0, targetMessage1.MsgSerial);
            Assert.Equal(0, targetMessage2.MsgSerial);
            Assert.Equal(0, targetMessage3.MsgSerial);
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        public void WhenReceivingAckOrNackMessage_ShouldHandleAction(ProtocolMessage.MessageAction action)
        {
            // Act
            bool result = _ackProcessor.OnMessageReceived(new ProtocolMessage(action));

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
        public void WhenReceivingNonAckOrNackMessage_ShouldNotHandleAction(ProtocolMessage.MessageAction action)
        {
            // Act
            bool result = _ackProcessor.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void OnAckReceivedForAMessage_AckCallbackCalled()
        {
            // Arrange
            var callbacks = new List<Tuple<bool, ErrorInfo>>();
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
            Action<bool, ErrorInfo> callback = (ack, err) =>
            {
                callbacks.Add(Tuple.Create(ack, err));
            };

            // Act
            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { MsgSerial = 0, count = 1 });
            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { MsgSerial = 1, count = 1 });

            // Assert
            Assert.Equal(2, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
            Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
        }

        [Fact]
        public void WhenSendingMessage_AckCallbackCalled_ForMultipleMessages()
        {
            // Arrange
            AcknowledgementProcessor target = new AcknowledgementProcessor(new Connection(new AblyRealtime(ValidKey)));
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();

            // Act
            target.QueueIfNecessary(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 0) callbacks.Add(Tuple.Create(ack, err)); });
            target.QueueIfNecessary(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 1) callbacks.Add(Tuple.Create(ack, err)); });
            target.QueueIfNecessary(new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test"), (ack, err) => { if (callbacks.Count == 2) callbacks.Add(Tuple.Create(ack, err)); });
            target.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Ack) { MsgSerial = 0, count = 3 });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1)); // Ack
            Assert.True(callbacks.TrueForAll(c => c.Item2 == null)); // No error
        }

        [Fact]
        public void WithNackMessageReceived_CallbackIsCalledWithError
            ()
        {
            // Arrange
            var callbacks = new List<Tuple<bool, ErrorInfo>>();
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
            Action<bool, ErrorInfo> callback = (ack, err) => { callbacks.Add(Tuple.Create(ack, err)); };
            // Act

            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = 0, count = 1 });
            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = 1, count = 1 });

            // Assert
            Assert.Equal(2, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => c.Item1 == false)); // Nack
            Assert.True(callbacks.TrueForAll(c => c.Item2 != null)); // Error
        }

        [Fact]
        public void WhenNackReceivedForMultipleMessage_AllCallbacksAreCalledAndErrorMessagePassed()
        {
            // Arrange
            var callbacks = new List<Tuple<bool, ErrorInfo>>();
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "Test");
            Action<bool, ErrorInfo> callback = (ack, err) => { callbacks.Add(Tuple.Create(ack, err)); };
            ErrorInfo error = new ErrorInfo("reason", 123);

            // Act
            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.QueueIfNecessary(message, callback);
            _ackProcessor.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Nack) { MsgSerial = 0, count = 3, error = error });

            // Assert
            Assert.Equal(3, callbacks.Count);
            Assert.True(callbacks.TrueForAll(c => !c.Item1)); // Nack
            Assert.True(callbacks.TrueForAll(c => ReferenceEquals(c.Item2, error))); // Error
        }

        

        

        public AckProtocolTests(ITestOutputHelper output) : base(output)
        {
            _connection = new Connection(new AblyRealtime(ValidKey));
            _connection.Initialise();
            _ackProcessor = new AcknowledgementProcessor(_connection);
        }
    }
}
