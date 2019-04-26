using System;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public class ChannelStateChange : EventArgs
    {
        public ChannelStateChange(ChannelEvent e, ChannelState state, ChannelState previous, ErrorInfo error = null, ProtocolMessage protocolMessage = null)
            : this(e, state, previous, error, false)
        {
            ProtocolMessage = protocolMessage;
            Resumed = protocolMessage != null && protocolMessage.HasFlag(ProtocolMessage.Flag.Resumed);
        }

        public ChannelStateChange(ChannelEvent e, ChannelState state, ChannelState previous, ErrorInfo error = null, bool resumed = false)
        {
            Event = e;
            Previous = previous;
            Current = state;
            Error = error;
            Resumed = resumed;
        }

        public ChannelState Previous { get; }

        public ChannelState Current { get; }

        public ErrorInfo Error { get; }

        public bool Resumed { get; }

        public ChannelEvent Event { get; }

        internal ProtocolMessage ProtocolMessage { get; set; } = null;
    }
}
