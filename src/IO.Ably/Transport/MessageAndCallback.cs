using System;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal class MessageAndCallback
    {
        public long Serial => Message.MsgSerial;
        public ProtocolMessage Message { get;  }
        public Action<bool, ErrorInfo> Callback { get; }

        public MessageAndCallback(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            Message = message;
            Callback = callback;
        }

        protected bool Equals(MessageAndCallback other)
        {
            return Equals(Message.MsgSerial, other.Message.MsgSerial);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageAndCallback) obj);
        }

        public override int GetHashCode()
        {
            return Message?.MsgSerial.GetHashCode() ?? 0;
        }
    }
}