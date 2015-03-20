using Ably.Types;
using System;

namespace Ably.Realtime
{
    public class ChannelStateChangedEventArgs : EventArgs
    {
        public ChannelStateChangedEventArgs(ChannelState state)
            : this(state, null) { }

        public ChannelStateChangedEventArgs(ChannelState state, ErrorInfo reason)
        {
            this.NewState = state;
            this.Reason = reason;
        }

        public ChannelState NewState { get; private set; }

        public ErrorInfo Reason { get; set; }
    }
}
