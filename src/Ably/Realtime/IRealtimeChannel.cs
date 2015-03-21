using System;

namespace Ably.Realtime
{
    public interface IRealtimeChannel : IChannel
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        ChannelState State { get; }

        void Attach();
        void Detach();
    }
}
