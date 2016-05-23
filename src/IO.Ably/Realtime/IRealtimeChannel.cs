﻿using System;
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
        event EventHandler<ChannelStateChangedEventArgs> StateChanged;
        event EventHandler<ChannelErrorEventArgs> Error;

        void Attach(Action<TimeSpan, ErrorInfo> callback = null);

        Task<Result<TimeSpan>> AttachAsync();
            
        void Detach(Action<TimeSpan, ErrorInfo> callback = null);

        Task<Result<TimeSpan>> DetachAsync();

        /// <summary>Subscribe a listener to all messages.</summary>
        void Subscribe(Action<Message> handler);

        /// <summary>Subscribe a listener to only messages whose name member matches the string name.</summary>
        /// <param name="eventName"></param>
        /// <param name="handler"></param>
        void Subscribe(string eventName, Action<Message> handler);

        bool Unsubscribe(Action<Message> handler);
        bool Unsubscribe(string eventName, Action<Message> handler);
        void Publish(string name, object data, Action<bool, ErrorInfo> callback = null, string clientId = null);
        Task<Result> PublishAsync(string eventName, object data, string clientId = null); 
        void Publish(Message message, Action<bool, ErrorInfo> callback = null);
        Task<Result> PublishAsync(Message message);
        void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback = null);
        Task<Result> PublishAsync(IEnumerable<Message> messages);
        Task<PaginatedResult<Message>> History(bool untilAttached = false);
        Task<PaginatedResult<Message>> History(DataRequestQuery dataQuery, bool untilAttached = false);
    }
}