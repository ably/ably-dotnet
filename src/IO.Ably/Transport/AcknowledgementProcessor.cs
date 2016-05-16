using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal interface IAcknowledgementProcessor
    {
        void QueueIfNecessary(ProtocolMessage message, Action<bool, ErrorInfo> callback);
        bool OnMessageReceived(ProtocolMessage message);
        IEnumerable<ProtocolMessage> GetQueuedMessages();
        void ClearQueueAndFailMessages(ErrorInfo error);
    }

    internal class AcknowledgementProcessor : IAcknowledgementProcessor
    {
        private readonly Connection _connection;
        private readonly Queue<MessageAndCallback> _queue = new Queue<MessageAndCallback>();
        private object _syncObject = new object();

        public IEnumerable<ProtocolMessage> GetQueuedMessages()
        {
            List<ProtocolMessage> messages;
            lock (_syncObject)
            {
                messages = new List<ProtocolMessage>(_queue.Select(x => x.Message));
            }
            return messages;
        }

        public AcknowledgementProcessor(Connection connection)
        {
            _connection = connection;
        }

        public void QueueIfNecessary(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            if (message.AckRequired)
                lock (_syncObject)
                {
                    message.MsgSerial = _connection.MessageSerial++;
                    _queue.Enqueue(new MessageAndCallback(message, callback));
                }
        }

        public bool OnMessageReceived(ProtocolMessage message)
        {
            if (message.action == ProtocolMessage.MessageAction.Ack ||
                message.action == ProtocolMessage.MessageAction.Nack)
            {
                HandleMessageAcknowledgement(message);
                return true;
            }
            return false;
        }

        public void ClearQueueAndFailMessages(ErrorInfo error)
        {
            lock (_syncObject)
            {
                foreach (var item in _queue.Where(x => x.Callback != null))
                {
                    var messageError = error ?? ErrorInfo.ReasonUnknown;
                    SafeExecute(item, false, messageError);
                }
                Reset();
            }
        }

        private void Reset()
        {
            _queue.Clear();
        }

        private void HandleMessageAcknowledgement(ProtocolMessage message)
        {
            lock (_syncObject)
            {
                var endSerial = message.MsgSerial + (message.count - 1);
                while (_queue.Count > 0)
                {
                    var current = _queue.Peek();
                    if (current.Serial <= endSerial)
                    {
                        if (message.action == ProtocolMessage.MessageAction.Ack)
                        {
                            SafeExecute(current, true, null);
                        }
                        else
                        {
                            SafeExecute(current, false, message.error ?? ErrorInfo.ReasonUnknown);
                        }
                        _queue.Dequeue();
                    }
                }
            }
        }

        private void SafeExecute(MessageAndCallback info, bool success, ErrorInfo error)
        {
            try
            {
                info.Callback?.Invoke(success, error);
            }
            catch (Exception)
            {
                var result = success ? "Success" : "Failed";
                var errorMessage = error != null ? $"Error: {error}" : "";
                Logger.Error($"Error executing callback for message with serial {info.Message.MsgSerial}. Result: {result}. {errorMessage}");
            }
        }
    }
}