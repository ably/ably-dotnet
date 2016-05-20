using System;

namespace IO.Ably.Realtime
{
    public class ChannelErrorEventArgs : EventArgs
    {
        public ErrorInfo Reason { get; }

        public ChannelErrorEventArgs(ErrorInfo reason)
        {
            Reason = reason;
        }
    }
}