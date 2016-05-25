using System;

namespace IO.Ably.Realtime
{
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelStateChangedEventArgs(ChannelState state, ErrorInfo reason = null)
        {
            NewState = state;
            Reason = reason;
        }

        public ChannelState NewState { get; }

        public ErrorInfo Reason { get; }
    }
}