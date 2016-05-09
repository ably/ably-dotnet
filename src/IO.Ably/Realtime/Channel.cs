using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Rest;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    /// <summary>Implement realtime channel.</summary>
    internal class RealtimeChannel : IRealtimeChannel
    {
        private readonly IConnectionManager _connection;
        private readonly Handlers _handlers = new Handlers();
        private readonly Dictionary<string, Handlers> _specificHandlers = new Dictionary<string, Handlers>();

        private readonly object _lockQueue = new object();

        private readonly object _lockSubscribers = new object();

        private List<QueuedProtocolMessage> _queuedMessages;

        internal RealtimeChannel(string name, string clientId, IConnectionManager connection)
        {
            Name = name;
            Presence = new Presence(connection, this, clientId);
            _connection = connection;
            _connection.MessageReceived += OnConnectionMessageReceived;
        }

        public event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        public ChannelOptions Options { get; set; }

        /// <summary>
        ///     The channel name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Indicates the current state of this channel.
        /// </summary>
        public ChannelState State { get; private set; }

        public Presence Presence { get; }

        /// <summary>
        ///     Attach to this channel. Any resulting channel state change will be indicated to any registered
        ///     <see cref="ChannelStateChanged" /> listener.
        /// </summary>
        public void Attach()
        {
            if (State == ChannelState.Attaching || State == ChannelState.Attached)
            {
                return;
            }

            if (!_connection.IsActive)
            {
                //TODO: This is backwards. Need to get it fixed
                _connection.Connection.Connect();
            }

            SetChannelState(ChannelState.Attaching);
            _connection.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, Name), null);
        }

        /// <summary>
        ///     Detach from this channel. Any resulting channel state change will be indicated to any registered
        ///     <see cref="ChannelStateChanged" /> listener.
        /// </summary>
        public void Detach()
        {
            if (State == ChannelState.Initialized || State == ChannelState.Detaching ||
                State == ChannelState.Detached)
            {
                return;
            }

            if (State == ChannelState.Failed)
            {
                throw new AblyException("Channel is Failed");
            }

            SetChannelState(ChannelState.Detaching);
            _connection.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Detach, Name), null);
        }

        public void Subscribe(IMessageHandler handler)
        {
            lock (_lockSubscribers)
                _handlers.Add(handler);
        }

        public void Subscribe(string eventName, IMessageHandler handler)
        {
            lock (_lockSubscribers)
            {
                Handlers handlers;
                if (!_specificHandlers.TryGetValue(eventName, out handlers))
                {
                    handlers = new Handlers();
                    _specificHandlers.Add(eventName, handlers);
                }
                handlers.Add(handler);
            }
        }

        public void Unsubscribe(IMessageHandler handler)
        {
            lock (_lockSubscribers)
            {
                if (_handlers.Remove(handler))
                    return;
            }
            Logger.Warning("Unsubscribe failed: was not subscribed");
        }

        public void Unsubscribe(string eventName, IMessageHandler handler)
        {
            lock (_lockSubscribers)
            {
                Handlers handlers;
                if (_specificHandlers.TryGetValue(eventName, out handlers))
                    if (handlers.Remove(handler))
                        return;
            }
            Logger.Warning("Unsubscribe failed: was not subscribed");
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        /// <param name="name">The event name.</param>
        /// <param name="data">The payload of the message.</param>
        public void Publish(string name, object data)
        {
            PublishAsync(name, data).IgnoreExceptions();
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        public Task PublishAsync(string name, object data)
        {
            return PublishAsync(new[] {new Message(name, data)});
        }

        /// <summary>Publish several messages on this channel.</summary>
        public void Publish(IEnumerable<Message> messages)
        {
            PublishAsync(messages).IgnoreExceptions();
        }

        /// <summary>Publish several messages on this channel.</summary>
        public Task PublishAsync(IEnumerable<Message> messages)
        {
            var tw = new TaskWrapper();
            PublishImpl(messages, tw.Callback);
            return tw.Task;
        }

        private void PublishImpl(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback)
        {
            // Create protocol message
            var msg = new ProtocolMessage(ProtocolMessage.MessageAction.Message, Name);
            msg.messages = messages.ToArray();

            if (State == ChannelState.Initialized || State == ChannelState.Attaching)
            {
                // Not connected, queue the message
                lock (_lockQueue)
                {
                    if (null == _queuedMessages)
                        _queuedMessages = new List<QueuedProtocolMessage>(16);
                    _queuedMessages.Add(new QueuedProtocolMessage(msg, callback));
                    return;
                }
            }
            if (State == ChannelState.Attached)
            {
                // Connected, send right now
                _connection.Send(msg, callback);
                return;
            }

            // Invalid state, throw
            throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000,
                HttpStatusCode.BadRequest));
        }

        protected void SetChannelState(ChannelState state)
        {
            State = state;
            OnChannelStateChanged(new ChannelStateChangedEventArgs(state));
        }

        private void OnChannelStateChanged(ChannelStateChangedEventArgs eventArgs)
        {
            if (ChannelStateChanged != null)
            {
                ChannelStateChanged(this, eventArgs);
            }
        }

        private void OnConnectionMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Attached:
                    if (State == ChannelState.Attaching)
                    {
                        SetChannelState(ChannelState.Attached);
                        SendQueuedMessages();
                    }
                    break;
                case ProtocolMessage.MessageAction.Detached:
                    if (State == ChannelState.Detaching)
                        SetChannelState(ChannelState.Detached);
                    break;
                case ProtocolMessage.MessageAction.Message:
                    OnMessage(message);
                    break;
                case ProtocolMessage.MessageAction.Error:
                    SetChannelState(ChannelState.Failed);
                    break;
                default:
                    Logger.Error("Channel::OnConnectionMessageReceived(): Unexpected message action {0}", message.action);
                    break;
            }
        }

        private void OnMessage(ProtocolMessage message)
        {
            foreach (var msg in message.messages)
            {
                lock (_lockSubscribers)
                {
                    foreach (var h in _handlers.GetAliveHandlers())
                        h.Handle(msg);

                    Handlers handlers;
                    if (!_specificHandlers.TryGetValue(msg.name, out handlers))
                        continue;
                    foreach (var h in handlers.GetAliveHandlers())
                        h.Handle(msg);
                }
            }
        }

        private int SendQueuedMessages()
        {
            List<QueuedProtocolMessage> list = null;
            lock (_lockQueue)
            {
                if (_queuedMessages == null || _queuedMessages.Count <= 0)
                    return 0;

                // Swap the list.
                list = _queuedMessages;
                _queuedMessages = null;
            }

            foreach (var qpm in list)
                _connection.Send(qpm.message, qpm.callback);
            return list.Count;
        }

        internal class QueuedProtocolMessage
        {
            public readonly Action<bool, ErrorInfo> callback;

            public readonly ProtocolMessage message;

            public QueuedProtocolMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback)
            {
                this.message = message;
                this.callback = callback;
            }
        }
    }
}