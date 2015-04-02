using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    public interface IRealtimeChannel : Ably.Rest.IChannel
    {
        event Action<Message[]> MessageReceived;
        event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        ChannelState State { get; }

        void Attach();
        void Detach();
        void Subscribe(string eventName, Action<Message[]> listener);
        void Unsubscribe(string eventName, Action<Message[]> listener);
        void Publish(string eventName, object data, Action<ErrorInfo> callback);
        void Publish(IEnumerable<Message> messages, Action<ErrorInfo> callback);
    }
}
