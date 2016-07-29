using System;

namespace IO.Ably.Realtime
{
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelStateChangedEventArgs(ChannelState state, ChannelState previous, ErrorInfo error = null)
        {
            Previous = previous;
            Current = state;
            Error = error;
        }

        public ChannelState Previous { get; }
        public ChannelState Current { get; }

        public ErrorInfo Error { get; }
    }
}