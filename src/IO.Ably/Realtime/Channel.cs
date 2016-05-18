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
        private readonly IConnectionManager _connectionManager;
        private Connection Connection => _connectionManager.Connection;
        private ConnectionStateType ConnectionState => Connection.State;
        public string AttachedSerial { get; set; }
        private readonly Handlers _handlers = new Handlers();

        private readonly object _lockQueue = new object();

        private readonly object _lockSubscribers = new object();

        private List<MessageAndCallback> _queuedMessages;
        public ErrorInfo Reason { get; internal set; }

        internal RealtimeChannel(string name, string clientId, IConnectionManager connectionManager)
        {
            Name = name;
            Presence = new Presence(connectionManager, this, clientId);
            _connectionManager = connectionManager;
            State = ChannelState.Initialized;
            SubscribeToConnectionEvents();
        }

        private void SubscribeToConnectionEvents()
        {
            _connectionManager.Connection.ConnectionStateChanged += ConnectionOnConnectionStateChanged;
        }

        private void ConnectionOnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs args)
        {
            switch (args.CurrentState)
            {
                case ConnectionStateType.Closed:
                    if (State == ChannelState.Attached || State == ChannelState.Attaching)
                        SetChannelState(ChannelState.Detaching);
                    break;
                case ConnectionStateType.Suspended:
                    if (State == ChannelState.Attached || State == ChannelState.Attaching)
                    {
                        SetChannelState(ChannelState.Detaching, ErrorInfo.ReasonSuspended);
                    }
                    break;
                case ConnectionStateType.Failed:
                    if (State != ChannelState.Detached || State != ChannelState.Initialized ||
                        State != ChannelState.Failed)
                    {
                        SetChannelState(ChannelState.Failed, ErrorInfo.ReasonFailed);
                    }
                    break;
            }
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

            SetChannelState(ChannelState.Attaching);
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
        }

        public void Subscribe(IMessageHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Subscribe(string eventName, IMessageHandler handler)
        {
            _handlers.Add(eventName, handler);
        }

        public void Unsubscribe(IMessageHandler handler)
        {
            if (_handlers.Remove(handler) == false)
                Logger.Warning("Unsubscribe failed: was not subscribed");
        }

        public void Unsubscribe(string eventName, IMessageHandler handler = null)
        {
            if (_handlers.Remove(eventName, handler) == false)
                Logger.Warning("Unsubscribe failed: was not subscribed");
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        /// <param name="name">The event name.</param>
        /// <param name="data">The payload of the message.</param>
        /// <param name="callback"></param>
        public void Publish(string name, object data, Action<bool, ErrorInfo> callback = null)
        {
            PublishImpl(new[] { new Message(name, data) }, callback);
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        public Task<Result> PublishAsync(string name, object data)
        {
            return PublishAsync(new[] { new Message(name, data) });
        }

        /// <summary>Publish several messages on this channel.</summary>
        public void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback = null)
        {
            PublishImpl(messages, callback);
        }

        /// <summary>Publish several messages on this channel.</summary>
        public Task<Result> PublishAsync(IEnumerable<Message> messages)
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
                if(State == ChannelState.Initialized)
                    Attach();
                // Not connected, queue the message
                lock (_lockQueue)
                {
                    if (_queuedMessages == null)
                        _queuedMessages = new List<MessageAndCallback>(16);
                    _queuedMessages.Add(new MessageAndCallback(msg, callback));
                    return;
                }
            }

            if (State == ChannelState.Attached)
            {
                // Connected, send right now
                _connectionManager.Send(msg, callback);
                return;
            }

            // Invalid state, throw
            throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000,
                HttpStatusCode.BadRequest));
        }

        internal void SetChannelState(ChannelState state, ProtocolMessage protocolMessage)
        {
            SetChannelState(state, protocolMessage.error, protocolMessage);
        }

        internal void SetChannelState(ChannelState state, ErrorInfo error = null, ProtocolMessage protocolMessage = null)
        {
            if (Logger.IsDebug)
            {
                var errorMessage = error != null ? "Error: " + error : "";
                Logger.Debug($"#{Name}: Changing state to: '{state}'. {errorMessage}");
            }

            HandleStateChange(state, error, protocolMessage);

            //TODO: Post the event back on the user's thread
            ChannelStateChanged?.Invoke(this, new ChannelStateChangedEventArgs(state, error));
        }

        private void HandleStateChange(ChannelState state, ErrorInfo error, ProtocolMessage protocolMessage)
        {
            State = state;
            Reason = error; //Set or clear the error on the channel

            switch (state)
            {
                case ChannelState.Attaching:
                    if (ConnectionState == ConnectionStateType.Initialized)
                    {
                        Connection.Connect();
                    }

                    //Even thought the connection won't have connected yet the message will be queued and sent as soon as
                    //the connection is made
                    _connectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, Name), null);
                    break;
                case ChannelState.Attached:
                    if (protocolMessage != null)
                    {
                        if (protocolMessage.HasPresenceFlag)
                        {
                            //Start sync
                        }
                        else
                        {
                            //Presence is in sync
                        }

                        AttachedSerial = protocolMessage.channelSerial;
                    }
                    SendQueuedMessages();
                    
                    break;
                case ChannelState.Detaching:
                    if (ConnectionState == ConnectionStateType.Closed || ConnectionState == ConnectionStateType.Connecting ||
                        ConnectionState == ConnectionStateType.Suspended)
                        SetChannelState(ChannelState.Detached, error);
                    else
                    {
                        _connectionManager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Detach, Name), null);
                    }
                    break;
                case ChannelState.Detached:
                    _connectionManager.FailMessageWaitingForAckAndClearOutgoingQueue(this, error);
                    break;
                case ChannelState.Failed:
                    _connectionManager.FailMessageWaitingForAckAndClearOutgoingQueue(this, error);
                    break;
            }
        }


        internal void OnMessage(Message message)
        {
            foreach (var handler in _handlers.GetHandlers())
            {
                SafeHandle(handler, message);
            }
            if (message.name.IsNotEmpty())
            {
                foreach (var specificHandler in _handlers.GetHandlers(message.name))
                {
                    SafeHandle(specificHandler, message);
                }
            }
        }

        private void SafeHandle(IMessageHandler handler, Message message)
        {
            try
            {
                handler.Handle(message);
            }
            catch (Exception ex)
            {
                Logger.Error("Error notifying subscriber", ex);
            }

        }

        private int SendQueuedMessages()
        {
            List<MessageAndCallback> list = null;
            lock (_lockQueue)
            {
                if (_queuedMessages == null || _queuedMessages.Count <= 0)
                    return 0;

                // Swap the list.
                list = _queuedMessages;
                _queuedMessages = null;
            }

            foreach (var qpm in list)
                _connectionManager.Send(qpm.Message, qpm.Callback);
            return list.Count;
        }
    }
}