using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    public interface IRealtimeChannel : IEventEmitter<ChannelEvent, ChannelStateChange>
    {
        ChannelState State { get; }

        string Name { get; }

        Presence Presence { get; }

        ChannelOptions Options { get; }

        ErrorInfo ErrorReason { get; }

        event EventHandler<ChannelStateChange> StateChanged;

        event EventHandler<ChannelErrorEventArgs> Error;

        void Attach(Action<bool, ErrorInfo> callback = null);

        Task<Result> AttachAsync();

        void Detach(Action<bool, ErrorInfo> callback = null);

        Task<Result> DetachAsync();

        /// <summary>Subscribe a listener to all messages.</summary>
        void Subscribe(Action<Message> handler);

        /// <summary>Subscribe a listener to only messages whose name member matches the string name.</summary>
        /// <param name="eventName"></param>
        /// <param name="handler"></param>
        void Subscribe(string eventName, Action<Message> handler);

        void Unsubscribe(Action<Message> handler);

        void Unsubscribe(string eventName, Action<Message> handler);

        void Unsubscribe();

        void Publish(string name, object data, Action<bool, ErrorInfo> callback = null, string clientId = null);

        Task<Result> PublishAsync(string eventName, object data, string clientId = null);

        void Publish(Message message, Action<bool, ErrorInfo> callback = null);

        Task<Result> PublishAsync(Message message);

        void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback = null);

        Task<Result> PublishAsync(IEnumerable<Message> messages);

        Task<PaginatedResult<Message>> HistoryAsync(bool untilAttach = false);

        Task<PaginatedResult<Message>> HistoryAsync(PaginatedRequestParams query, bool untilAttach = false);
    }
}
