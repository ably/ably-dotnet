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
        ErrorInfo Reason { get; }
        event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        void Attach(Action<TimeSpan, ErrorInfo> callback = null);

        Task<Result<TimeSpan>> AttachAsync();
            
        void Detach();

        /// <summary>Subscribe a listener to all messages.</summary>
        void Subscribe(IMessageHandler handler);

        /// <summary>Subscribe a listener to only messages whose name member matches the string name.</summary>
        /// <param name="eventName"></param>
        /// <param name="handler"></param>
        void Subscribe(string eventName, IMessageHandler handler);

        void Unsubscribe(IMessageHandler handler);
        void Unsubscribe(string eventName, IMessageHandler handler);
        void Publish(string name, object data, Action<bool, ErrorInfo> callback = null);
        Task<Result> PublishAsync(string eventName, object data); 
        void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback = null);
        Task<Result> PublishAsync(IEnumerable<Message> messages);
    }
}