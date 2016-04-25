using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.Rest;

namespace IO.Ably.Realtime
{
    public interface IRealtimeChannel
    {
        ChannelState State { get; }
        string Name { get; }
        Presence Presence { get; }
        ChannelOptions Options { get; set; }
        // event Action<Message[]> MessageReceived;
        event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        void Attach();
        void Detach();

        /// <summary>Subscribe a listener to all messages.</summary>
        void Subscribe(IMessageHandler handler);

        /// <summary>Subscribe a listener to only messages whose name member matches the string name.</summary>
        /// <param name="eventName"></param>
        /// <param name="handler"></param>
        void Subscribe(string eventName, IMessageHandler handler);

        void Unsubscribe(IMessageHandler handler);
        void Unsubscribe(string eventName, IMessageHandler handler);
        void Publish(string name, object data);
        Task PublishAsync(string eventName, object data);
        void Publish(IEnumerable<Message> messages);
        Task PublishAsync(IEnumerable<Message> messages);
    }
}