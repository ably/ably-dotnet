using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    public interface IRealtimeChannel
    {
        event Action<Message[]> MessageReceived;
        event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        ChannelState State { get; }
        string Name { get; }
        Presence Presence { get; }

        void Attach();
        void Detach();
        void Subscribe(string eventName, Action<Message[]> listener);
        void Unsubscribe(string eventName, Action<Message[]> listener);
        void Publish(string name, object data);
        void Publish(string eventName, object data, Action<bool, ErrorInfo> callback);
        void Publish(IEnumerable<Message> messages);
        void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback);
    }
}
