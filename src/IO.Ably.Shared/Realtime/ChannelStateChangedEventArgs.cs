using System;

namespace IO.Ably.Realtime
{
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelStateChangedEventArgs(ChannelState state, ErrorInfo error = null)
        {
            Current = state;
            Error = error;
        }

        public ChannelState Current { get; }

        public ErrorInfo Error { get; }
    }
}