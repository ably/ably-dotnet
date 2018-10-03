using System;

namespace IO.Ably.Realtime
{
    internal class QueuedPresenceMessage
    {
        public QueuedPresenceMessage(PresenceMessage message, Action<bool, ErrorInfo> callback)
        {
            Message = message;
            Callback = callback;
        }

        public PresenceMessage Message { get; }

        public Action<bool, ErrorInfo> Callback { get; }
    }
}