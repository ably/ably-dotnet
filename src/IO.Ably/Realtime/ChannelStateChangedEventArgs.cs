using System;

namespace IO.Ably.Realtime
{
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelStateChangedEventArgs(ChannelState state)
            : this(state, null)
        {
        }

        public ChannelStateChangedEventArgs(ChannelState state, ErrorInfo reason)
        {
            NewState = state;
            Reason = reason;
        }

        public ChannelState NewState { get; private set; }

        public ErrorInfo Reason { get; set; }
    }
}