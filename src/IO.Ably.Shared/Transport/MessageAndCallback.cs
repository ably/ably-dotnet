using System;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal class MessageAndCallback
    {
        public long Serial => Message.msgSerial;
        public ProtocolMessage Message { get;  }
        public Action<bool, ErrorInfo> Callback { get; }

        public MessageAndCallback(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            Message = message;
            Callback = callback;
        }

        protected bool Equals(MessageAndCallback other)
        {
            return Equals(Message.msgSerial, other.Message.msgSerial);
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
            return Message?.msgSerial.GetHashCode() ?? 0;
        }
    }

    internal static class MessageAndCallbackExtensions
    {
        public static void SafeExecute(this MessageAndCallback info, bool success, ErrorInfo error)
        {
            try
            {
                info.Callback?.Invoke(success, error);
            }
            catch (Exception)
            {
                var result = success ? "Success" : "Failed";
                var errorMessage = error != null ? $"Error: {error}" : "";
                Logger.Error($"Error executing callback for message with serial {info.Message.msgSerial}. Result: {result}. {errorMessage}");
            }
        }
    }
}