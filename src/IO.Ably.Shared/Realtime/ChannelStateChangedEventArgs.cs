using System;

namespace IO.Ably.Realtime
{
    public class ChannelStateChange : EventArgs
    {
        public ChannelStateChange(ChannelState state, ChannelState previous, ErrorInfo error = null, bool resumed = false)
        {
            Previous = previous;
            Current = state;
            Error = error;
            Resumed = resumed;
        }

        public ChannelState Previous { get; }

        public ChannelState Current { get; }

        public ErrorInfo Error { get; }

        public bool Resumed { get;  }
    }
}
