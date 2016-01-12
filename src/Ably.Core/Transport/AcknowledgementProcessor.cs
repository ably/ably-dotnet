using Ably.Transport.States.Connection;
using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Transport
{
    internal interface IAcknowledgementProcessor
    {
        void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback);
        bool OnMessageReceived(ProtocolMessage message);
        void OnStateChanged(ConnectionState state);
    }

    internal class AcknowledgementProcessor : IAcknowledgementProcessor
    {
        public AcknowledgementProcessor()
        {
            this.msgSerial = 0;
            this.ackQueue = new Dictionary<long, Action<bool, ErrorInfo>>();
        }

        private long msgSerial;
        private Dictionary<long, Action<bool, ErrorInfo>> ackQueue;

        public void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            if (!AckRequired(message))
                return;

            message.msgSerial = this.msgSerial++;

            if (callback != null)
            {
                this.ackQueue.Add(message.msgSerial, callback);
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
                    this.Reset();
                    break;
                case Realtime.ConnectionState.Closed:
                case Realtime.ConnectionState.Failed:
                    {
                        foreach (var item in this.ackQueue)
                        {
                            item.Value(false, state.Error ?? ErrorInfo.ReasonUnknown);
                        }
                        this.ackQueue.Clear();
                        break;
                    }
            }
        }

        private void Reset()
        {
            this.msgSerial = 0;
            this.ackQueue.Clear();
        }

        private static bool AckRequired(ProtocolMessage msg)
        {
            return (msg.action == ProtocolMessage.MessageAction.Message ||
                msg.action == ProtocolMessage.MessageAction.Presence);
        }

        private void HandleMessageAcknowledgement(ProtocolMessage message)
        {
            long startSerial = message.msgSerial;
            long endSerial = message.msgSerial + (message.count - 1);
            for (long i = startSerial; i <= endSerial; i++)
            {
                Action<bool, ErrorInfo> callback;
                if (this.ackQueue.TryGetValue(i, out callback))
                {
                    if (message.action == ProtocolMessage.MessageAction.Ack)
                    {
                        callback(true, null);
                    }
                    else
                    {
                        callback(false, message.error ?? ErrorInfo.ReasonUnknown);
                    }
                    this.ackQueue.Remove(i);
                }
            }
        }
    }
}
