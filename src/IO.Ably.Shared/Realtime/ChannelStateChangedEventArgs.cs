using System;

namespace IO.Ably.Realtime
{
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelStateChangedEventArgs(ChannelState state, ErrorInfo error = null)
        {
            NewState = state;
            Error = error;
        }

        public ChannelState NewState { get; }

        public ErrorInfo Error { get; }
    }
}