using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    public interface IRealtimeChannel
    {
        event Action<Message[]> MessageReceived;
        event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        ChannelState State { get; }
        string Name { get; }
        Presence Presence { get; }
        Rest.ChannelOptions Options { get; set; }

        void Attach();
        void Detach();
        void Subscribe(string eventName, Action<Message[]> listener);
        void Unsubscribe(string eventName, Action<Message[]> listener);
        void Publish(string name, object data);
        Task PublishAsync(string eventName, object data);
        void Publish(IEnumerable<Message> messages);
        Task PublishAsync( IEnumerable<Message> messages );
    }
}
