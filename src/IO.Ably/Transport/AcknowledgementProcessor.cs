using System;
using System.Collections.Generic;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal interface IAcknowledgementProcessor
    {
        void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback);
        bool OnMessageReceived(ProtocolMessage message);
        void OnStateChanged(ConnectionState state);
    }

    internal class AcknowledgementProcessor : IAcknowledgementProcessor
    {
        //TODO: Look at replacing this with a ConcurrentDictionary
        private readonly Dictionary<long, Action<bool, ErrorInfo>> _ackQueue;

        private long msgSerial;

        public AcknowledgementProcessor()
        {
            msgSerial = 0;
            _ackQueue = new Dictionary<long, Action<bool, ErrorInfo>>();
        }

        public void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            if (!AckRequired(message))
                return;

            message.msgSerial = msgSerial++;

            if (callback != null)
            {
                _ackQueue.Add(message.msgSerial, callback);
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

        public void OnStateChanged(ConnectionState state)
        {
            switch (state.State)
            {
                case Realtime.ConnectionState.Connected:
                    Reset();
                    break;
                case Realtime.ConnectionState.Closed:
                case Realtime.ConnectionState.Failed:
                {
                    foreach (var item in _ackQueue)
                    {
                        item.Value(false, state.Error ?? ErrorInfo.ReasonUnknown);
                    }
                    _ackQueue.Clear();
                    break;
                }
            }
        }

        private void Reset()
        {
            msgSerial = 0;
            _ackQueue.Clear();
        }

        private static bool AckRequired(ProtocolMessage msg)
        {
            return msg.action == ProtocolMessage.MessageAction.Message ||
                   msg.action == ProtocolMessage.MessageAction.Presence;
        }

        private void HandleMessageAcknowledgement(ProtocolMessage message)
        {
            var startSerial = message.msgSerial;
            var endSerial = message.msgSerial + (message.count - 1);
            for (var i = startSerial; i <= endSerial; i++)
            {
                Action<bool, ErrorInfo> callback;
                if (_ackQueue.TryGetValue(i, out callback))
                {
                    if (message.action == ProtocolMessage.MessageAction.Ack)
                    {
                        callback(true, null);
                    }
                    else
                    {
                        callback(false, message.error ?? ErrorInfo.ReasonUnknown);
                    }
                    _ackQueue.Remove(i);
                }
            }
        }
    }
}