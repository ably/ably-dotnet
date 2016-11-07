using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IO.Ably.Rest;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    /// <summary>Implement realtime channel.</summary>
    internal class RealtimeChannel : EventEmitter<ChannelState, ChannelStateChange>, IRealtimeChannel, IDisposable
    {
        internal AblyRealtime RealtimeClient { get; }
        private IConnectionManager ConnectionManager => RealtimeClient.ConnectionManager;
        private Connection Connection => RealtimeClient.Connection;
        private ConnectionState ConnectionState => Connection.State;
        private readonly Handlers<Message> _handlers = new Handlers<Message>();
        private readonly CountdownTimer _timer;
        internal IRestChannel RestChannel => RealtimeClient.RestClient.Channels.Get(Name);
        private readonly object _lockQueue = new object();
        private readonly ChannelAwaiter _attachedAwaiter;
        private readonly ChannelAwaiter _detachedAwaiter;
        private ChannelOptions _options;

        public string AttachedSerial { get; set; }
        public List<MessageAndCallback> QueuedMessages { get; set; } = new List<MessageAndCallback>(16);
        public ErrorInfo ErrorReason { get; internal set; }

        public event EventHandler<ChannelStateChange> StateChanged = delegate { };
        public event EventHandler<ChannelErrorEventArgs> Error = delegate { };

        public ChannelOptions Options

        {
            get { return _options; }
            set { _options = value ?? new ChannelOptions(); }
        }

        public string Name { get; }

        /// <summary>
        ///     Indicates the current state of this channel.
        /// </summary>
        public ChannelState State { get; private set; }

        public Presence Presence { get; }

        internal RealtimeChannel(string name, string clientId, AblyRealtime realtimeClient, ChannelOptions options)
        {
            Name = name;
            Options = options;
            _timer = new CountdownTimer($"#{Name} timer");
            Presence = new Presence(realtimeClient.ConnectionManager, this, clientId);
            RealtimeClient = realtimeClient;
            State = ChannelState.Initialized;
            SubscribeToConnectionEvents();
            _attachedAwaiter = new ChannelAwaiter(this, ChannelState.Attached);
            _detachedAwaiter = new ChannelAwaiter(this, ChannelState.Detached);
        }

        private void SubscribeToConnectionEvents()
        {
            ConnectionManager.Connection.InternalStateChanged += InternalOnInternalStateChanged;
        }

        private void InternalOnInternalStateChanged(object sender, ConnectionStateChange connectionStateChange)
        {
            switch (connectionStateChange.Current)
            {
                case Realtime.ConnectionState.Closed:
                    if (State == ChannelState.Attached || State == ChannelState.Attaching)
                        SetChannelState(ChannelState.Detaching);
                    break;
                case Realtime.ConnectionState.Suspended:
                    if (State == ChannelState.Attached || State == ChannelState.Attaching)
                    {
                        SetChannelState(ChannelState.Detaching, ErrorInfo.ReasonSuspended);
                    }
                    break;
                case Realtime.ConnectionState.Failed:
                    if (State != ChannelState.Detached && State != ChannelState.Initialized &&
                        State != ChannelState.Failed)
                    {
                        SetChannelState(ChannelState.Failed, connectionStateChange.Reason ?? ErrorInfo.ReasonFailed);
                    }
                    break;
            }
        }

        

        /// <summary>
        ///     Attach to this channel. Any resulting channel state change will be indicated to any registered
        ///     <see cref="StateChanged" /> listener.
        /// </summary>
        public void Attach(Action<bool, ErrorInfo> callback = null)
        {
            if (State == ChannelState.Attaching || State == ChannelState.Attached)
            {
                callback?.Invoke(true, null);
                return;
            }

            _attachedAwaiter.Wait(callback);
            SetChannelState(ChannelState.Attaching);
        }

        public Task<Result> AttachAsync()
        {
            return TaskWrapper.Wrap(Attach);
        }

        private void OnAttachTimeout()
        {
            ConnectionManager.Execute(() =>
            {
                SetChannelState(ChannelState.Failed, new ErrorInfo("Channel didn't attach within the default timeout", 50000));
            });
        }

        private void OnDetachTimeout()
        {
            ConnectionManager.Execute(() =>
            {
                SetChannelState(ChannelState.Failed, new ErrorInfo("Channel didn't detach within the default timeout", 50000));
            });
        }

        /// <summary>
        ///     Detach from this channel. Any resulting channel state change will be indicated to any registered
        ///     <see cref="StateChanged" /> listener.
        /// </summary>
        public void Detach(Action<bool, ErrorInfo> callback = null)
        {
            if (State == ChannelState.Initialized || State == ChannelState.Detaching ||
                State == ChannelState.Detached)
            {
                callback?.Invoke(true, null);
                return;
            }

            if (State == ChannelState.Failed)
            {
                throw new AblyException("Channel is Failed");
            }

            _detachedAwaiter.Wait(callback);
            SetChannelState(ChannelState.Detaching);
        }

        public Task<Result> DetachAsync()
        {
            return TaskWrapper.Wrap(Detach);
        }

        public void Subscribe(Action<Message> handler)
        {
            if(State != ChannelState.Attached && State != ChannelState.Attaching)
                Attach();

            _handlers.Add(new MessageHandlerAction<Message>(handler));
        }

        public void Subscribe(string eventName, Action<Message> handler)
        {
            if (State != ChannelState.Attached && State != ChannelState.Attaching)
                Attach();

            _handlers.Add(eventName, handler.ToHandlerAction());
        }

        public void Unsubscribe(Action<Message> handler)
        {
            _handlers.Remove(handler.ToHandlerAction());
        }

        public void Unsubscribe(string eventName, Action<Message> handler)
        {
            _handlers.Remove(eventName, handler.ToHandlerAction());
        }

        public void Unsubscribe()
        {
            _handlers.RemoveAll();
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        /// <param name="name">The event name.</param>
        /// <param name="data">The payload of the message.</param>
        /// <param name="clientId"></param>
        /// <param name="callback"></param>
        public void Publish(string name, object data, Action<bool, ErrorInfo> callback = null, string clientId = null)
        {
            PublishImpl(new[] { new Message(name, data, clientId) }, callback);
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        public Task<Result> PublishAsync(string name, object data, string clientId = null)
        {
            return PublishAsync(new[] { new Message(name, data, clientId) });
        }

        public void Publish(Message message, Action<bool, ErrorInfo> callback = null)
        {
            Publish(new[] {message}, callback);
        }

        public Task<Result> PublishAsync(Message message)
        {
            return PublishAsync(new [] { message });
        }

        /// <summary>Publish several messages on this channel.</summary>
        public void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback = null)
        {
            PublishImpl(messages, callback);
        }

        /// <summary>Publish several messages on this channel.</summary>
        public async Task<Result> PublishAsync(IEnumerable<Message> messages)
        {
            var tw = new TaskWrapper();
            try
            {
                PublishImpl(messages, tw.Callback);
            }
            catch (Exception ex)
            {
                tw.SetException(ex);
            }
            var result = await Task.WhenAny(Task.Delay(RealtimeClient.Options.RealtimeRequestTimeout), tw.Task);
            if (result == tw.Task)
            {
                return tw.Task.Result;
            }
            return Result.Fail(new ErrorInfo("PublishAsync timeout expired. Message was not confirmed by the server"));
        }

        public Task<PaginatedResult<Message>> HistoryAsync(bool untilAttach = false)
        {
            var query = new HistoryRequestParams();
            if (untilAttach)
            {
                AddUntilAttachParameter(query);
            }
            return RestChannel.HistoryAsync(query);
        }

        public Task<PaginatedResult<Message>> HistoryAsync(HistoryRequestParams query, bool untilAttach = false)
        {
            query = query ?? new HistoryRequestParams();
            if (untilAttach)
            {
                AddUntilAttachParameter(query);
            }
                
            return RestChannel.HistoryAsync(query);
        }

        public void OnError(ErrorInfo error)
        {
            ErrorReason = error; //Set or clear the error

            RealtimeClient.NotifyExternalClients(() => Error.Invoke(this, new ChannelErrorEventArgs(error)));
        }

        public void Dispose()
        {
            _handlers.RemoveAll();
            Presence?.Dispose();
        }

        internal void AddUntilAttachParameter(HistoryRequestParams query)
        {
            if (State != ChannelState.Attached)
            {
                throw new AblyException("Channel is not attached. Cannot use untilAttach parameter");
            }
            query.ExtraParameters.Add("fromSerial", AttachedSerial);
        }

        private void PublishImpl(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback)
        {
            // Create protocol message
            var msg = new ProtocolMessage(ProtocolMessage.MessageAction.Message, Name);
            msg.Messages = messages.ToArray();

            if (State == ChannelState.Initialized || State == ChannelState.Attaching)
            {
                if(State == ChannelState.Initialized)
                    Attach();
                // Not connected, queue the message
                lock (_lockQueue)
                {
                    if(Logger.IsDebug) Logger.Debug($"#{Name}:{State} queuing message");
                    QueuedMessages.Add(new MessageAndCallback(msg, callback));
                    return;
                }
            }

            if (State == ChannelState.Attached)
            {
                // Connected, send right now
                SendMessage(msg, callback);
                return;
            }

            // Invalid state, throw
            throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000,
                HttpStatusCode.BadRequest));
        }

        internal void SetChannelState(ChannelState state, ProtocolMessage protocolMessage)
        {
            SetChannelState(state, protocolMessage.Error, protocolMessage);
        }

        internal void SetChannelState(ChannelState state, ErrorInfo error = null, ProtocolMessage protocolMessage = null)
        {
            if (Logger.IsDebug)
            {
                var errorMessage = error != null ? "Error: " + error : "";
                Logger.Debug($"#{Name}: Changing state to: '{state}'. {errorMessage}");
            }

            OnError(error);
            var previousState = State;
            HandleStateChange(state, error, protocolMessage);

            RealtimeClient.NotifyExternalClients(() =>
                {
                    var args = new ChannelStateChange(state, previousState, error);
                    try
                    {
                        StateChanged.Invoke(this, args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error notifying event handlers for state change: {state}", ex);
                    }
                    
                    Emit(state, args);
                });
        }

        private void HandleStateChange(ChannelState state, ErrorInfo error, ProtocolMessage protocolMessage)
        {
            State = state;

            switch (state)
            {
                case ChannelState.Attaching:
                    if (ConnectionState == Realtime.ConnectionState.Initialized)
                    {
                        Connection.Connect();
                    }

                    _timer.Abort();
                    _timer.Start(ConnectionManager.Options.RealtimeRequestTimeout, OnAttachTimeout);

                    //Even thought the connection won't have connected yet the message will be queued and sent as soon as
                    //the connection is made
                    SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, Name));
                    break;
                case ChannelState.Attached:

                    _timer.Abort();
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

                        AttachedSerial = protocolMessage.ChannelSerial;


                    }
                    SendQueuedMessages();
                    
                    break;
                case ChannelState.Detaching:
                    //Fail timer if still waiting for attached.
                    _attachedAwaiter.Fail(new ErrorInfo("Channel transitioned to detaching", 50000));

                    if (ConnectionState == Realtime.ConnectionState.Closed || ConnectionState == Realtime.ConnectionState.Connecting ||
                        ConnectionState == Realtime.ConnectionState.Suspended)
                        SetChannelState(ChannelState.Detached, error);
                    else
                    {
                        _timer.Start(ConnectionManager.Options.RealtimeRequestTimeout, OnDetachTimeout);
                        SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Detach, Name));
                    }

                    break;
                case ChannelState.Detached:
                    _timer.Abort();
                    ConnectionManager.FailMessageWaitingForAckAndClearOutgoingQueue(this, error);
                    ClearAndFailChannelQueuedMessages(error);
                    break;
                case ChannelState.Failed:
                    _attachedAwaiter.Fail(error);
                    _detachedAwaiter.Fail(error);
                    ConnectionManager.FailMessageWaitingForAckAndClearOutgoingQueue(this, error);
                    ClearAndFailChannelQueuedMessages(error);
                    break;
            }
        }

        private void ClearAndFailChannelQueuedMessages(ErrorInfo error)
        {
            lock (_lockQueue)
            {
                foreach (var messageAndCallback in QueuedMessages)
                {
                    messageAndCallback.SafeExecute(false, error);
                }
                QueuedMessages.Clear();
            }
        }

        internal void OnMessage(Message message)
        {
            foreach (var handler in _handlers.GetHandlers())
            {
                var loopHandler = handler;
                RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message));
            }

            if (message.Name.IsNotEmpty())
            {
                foreach (var specificHandler in _handlers.GetHandlers(message.Name))
                {
                    var loopHandler = specificHandler;
                    RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message));
                }
            }
        }

        private int SendQueuedMessages()
        {
            List<MessageAndCallback> list;
            lock (_lockQueue)
            {
                if (QueuedMessages.Count <= 0)
                    return 0;

                // Swap the list.
                list = new List<MessageAndCallback>(QueuedMessages);
                QueuedMessages.Clear();
            }

            foreach (var qpm in list)
                SendMessage(qpm.Message, qpm.Callback);
            return list.Count;
        }

        private void SendMessage(ProtocolMessage protocolMessage, Action<bool, ErrorInfo> callback = null)
        {
            ConnectionManager.Send(protocolMessage, callback, Options);
        }

        
    }
}