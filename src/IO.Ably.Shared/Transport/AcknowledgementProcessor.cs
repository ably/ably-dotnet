using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Realtime;
using IO.Ably;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal interface IAcknowledgementProcessor
    {
        void QueueIfNecessary(ProtocolMessage message, Action<bool, ErrorInfo> callback);
        bool OnMessageReceived(ProtocolMessage message);
        IEnumerable<ProtocolMessage> GetQueuedMessages();
        void ClearQueueAndFailMessages(ErrorInfo error);
        void FailChannelMessages(string name, ErrorInfo error);
    }

    internal class AcknowledgementProcessor : IAcknowledgementProcessor
    {
        internal ILogger Logger { get; private set; }

        private readonly Connection _connection;
        private readonly List<MessageAndCallback> _queue = new List<MessageAndCallback>();
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
            Logger = connection.Logger;
            _connection = connection;
        }

        public void QueueIfNecessary(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            if (message.AckRequired)
            {
                lock (_syncObject)
                {
                    message.MsgSerial = _connection.MessageSerial++;
                    _queue.Add(new MessageAndCallback(message, callback));
                    if(Logger.IsDebug)
                    {
                        Logger.Debug($"Message ({message.Action}) with serial ({message.MsgSerial}) was queued to get Ack");
                    }
                }
            }
        }

        public bool OnMessageReceived(ProtocolMessage message)
        {
            if (message.Action == ProtocolMessage.MessageAction.Ack ||
                message.Action == ProtocolMessage.MessageAction.Nack)
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
                    item.SafeExecute(false, messageError);
                }
                _queue.Clear();
            }
        }

        public void FailChannelMessages(string name, ErrorInfo error)
        {
            lock (_syncObject)
            {
                var messagesToRemove = _queue.Where(x => x.Message.Channel == name).ToList();
                foreach (var message in messagesToRemove)
                {
                    message.SafeExecute(false, error);
                    _queue.Remove(message);
                }
            }
        }

        private void HandleMessageAcknowledgement(ProtocolMessage message)
        {
            lock (_syncObject)
            {
                var endSerial = message.MsgSerial + (message.Count - 1);
                var listForProcessing = new List<MessageAndCallback>(_queue);
                foreach (var current in listForProcessing)
                {
                    if (current.Serial <= endSerial)
                    {
                        if (message.Action == ProtocolMessage.MessageAction.Ack)
                        {
                            current.SafeExecute(true, null);
                        }
                        else
                        {
                            current.SafeExecute(false, message.Error ?? ErrorInfo.ReasonUnknown);
                        }
                        _queue.Remove(current);
                    }
                }
            }
        }


    }
}