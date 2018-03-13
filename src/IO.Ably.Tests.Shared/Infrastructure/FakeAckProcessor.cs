using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    internal class FakeAckProcessor : IAcknowledgementProcessor
    {
        public bool QueueIfNecessaryCalled { get; set; }

        public bool OnMessageReceivedCalled { get; set; }

        public ConnectionStateBase LastState { get; set; }

        private Queue<MessageAndCallback> _queuedMessages = new Queue<MessageAndCallback>();

        public void QueueIfNecessary(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            QueueIfNecessaryCalled = true;
            _queuedMessages.Enqueue(new MessageAndCallback(message, callback));
        }

        public bool OnMessageReceived(ProtocolMessage message)
        {
            OnMessageReceivedCalled = true;
            return true;
        }

        public IEnumerable<ProtocolMessage> GetQueuedMessages()
        {
            return _queuedMessages.Select(x => x.Message);
        }

        public void ClearQueueAndFailMessages(ErrorInfo error)
        {
            QueueCleared = true;
            _queuedMessages.Clear();
        }

        public void FailChannelMessages(string name, ErrorInfo error)
        {
            FailChannelMessagesCalled = true;
        }

        public bool FailChannelMessagesCalled { get; set; }

        public bool QueueCleared { get; set; }
    }
}
