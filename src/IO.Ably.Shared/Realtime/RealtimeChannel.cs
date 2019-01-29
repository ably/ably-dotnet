using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.Rest;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    /// <summary>Implement realtime channel.</summary>
    internal class RealtimeChannel : EventEmitter<ChannelEvent, ChannelStateChange>, IRealtimeChannel, IDisposable
    {
        internal AblyRealtime RealtimeClient { get; }

        private IConnectionManager ConnectionManager => RealtimeClient.ConnectionManager;

        private Connection Connection => RealtimeClient.Connection;

        private ConnectionState ConnectionState => Connection.State;

        private readonly Handlers<Message> _handlers = new Handlers<Message>();

        internal IRestChannel RestChannel => RealtimeClient.RestClient.Channels.Get(Name);

        private readonly object _lockQueue = new object();

        internal ChannelAwaiter AttachedAwaiter { get; }

        internal ChannelAwaiter DetachedAwaiter { get; }

        private ChannelOptions _options;
        private ChannelState _state;

        protected override Action<Action> NotifyClient => RealtimeClient.NotifyExternalClients;

        public string AttachedSerial { get; set; }

        public List<MessageAndCallback> QueuedMessages { get; set; } = new List<MessageAndCallback>(16);

        public ErrorInfo ErrorReason { get; internal set; }

        public event EventHandler<ChannelStateChange> StateChanged = delegate { };

        internal event EventHandler<ChannelStateChange> InternalStateChanged = delegate { };

        public event EventHandler<ChannelErrorEventArgs> Error = delegate { };

        public ChannelOptions Options
        {
            get => _options;
            set => _options = value ?? new ChannelOptions();
        }

        public string Name { get; }

        /// <summary>
        ///     Indicates the current state of this channel.
        /// </summary>
        public ChannelState State
        {
            get => _state;
            internal set
            {
                if (value != _state)
                {
                    PreviousState = _state;
                    _state = value;
                }
            }
        }

        internal ChannelState PreviousState { get; set; }

        public Presence Presence { get; }

        internal RealtimeChannel(string name, string clientId, AblyRealtime realtimeClient, ChannelOptions options = null)
            : base(options?.Logger)
        {
            Name = name;
            Options = options;
            Presence = new Presence(realtimeClient.ConnectionManager, this, clientId, Logger);
            RealtimeClient = realtimeClient;
            State = ChannelState.Initialized;
            SubscribeToConnectionEvents();
            AttachedAwaiter = new ChannelAwaiter(this, ChannelState.Attached, Logger, OnAttachTimeout);
            DetachedAwaiter = new ChannelAwaiter(this, ChannelState.Detached, Logger, OnDetachTimeout);
        }

        private void SubscribeToConnectionEvents()
        {
            ConnectionManager.Connection.BeginConnect += ConnectionOnBeginConnect;
            ConnectionManager.Connection.InternalStateChanged += InternalOnInternalStateChanged;
        }

        private void UnSubscribeFromConnectionEvents()
        {
            ConnectionManager.Connection.BeginConnect -= ConnectionOnBeginConnect;
            ConnectionManager.Connection.InternalStateChanged -= InternalOnInternalStateChanged;
        }

        private void ConnectionOnBeginConnect(object sender, EventArgs e)
        {
            switch (ConnectionState)
            {
                case ConnectionState.Failed:
                    /* (RTN11d)
                     * If the [Connection] state is FAILED,
                     * transitions all the channels to INITIALIZED */
                    SetChannelState(ChannelState.Initialized);
                    break;
            }
        }

        internal void InternalOnInternalStateChanged(object sender, ConnectionStateChange connectionStateChange)
        {
            switch (connectionStateChange.Current)
            {
                case ConnectionState.Connected:
                    if (State == ChannelState.Suspended)
                    {
                        if (AttachedAwaiter.StartWait(null, ConnectionManager.Options.RealtimeRequestTimeout))
                        {
                            SetChannelState(ChannelState.Attaching);
                        }
                    }

                    if (State == ChannelState.Attaching)
                    {
                        if (AttachedAwaiter.StartWait(null, ConnectionManager.Options.RealtimeRequestTimeout))
                        {
                            SetChannelState(ChannelState.Attaching, true);
                        }
                    }

                    if (State == ChannelState.Detaching)
                    {
                        if (DetachedAwaiter.StartWait(null, ConnectionManager.Options.RealtimeRequestTimeout))
                        {
                            SetChannelState(ChannelState.Detaching, true);
                        }
                    }

                    break;
                case ConnectionState.Disconnected:
                    AttachedAwaiter.Fail(new ErrorInfo("Connection is Disconnected"));
                    DetachedAwaiter.Fail(new ErrorInfo("Connection is Disconnected"));
                    break;
                case ConnectionState.Closed:
                    AttachedAwaiter.Fail(new ErrorInfo("Connection is closed"));
                    DetachedAwaiter.Fail(new ErrorInfo("Connection is closed"));
                    if (State == ChannelState.Attached || State == ChannelState.Attaching)
                    {
                        SetChannelState(ChannelState.Detaching);
                    }

                    break;
                case ConnectionState.Suspended:
                    AttachedAwaiter.Fail(new ErrorInfo("Connection is suspended"));
                    DetachedAwaiter.Fail(new ErrorInfo("Connection is suspended"));
                    if (State == ChannelState.Attached || State == ChannelState.Attaching)
                    {
                        SetChannelState(ChannelState.Suspended, ErrorInfo.ReasonSuspended);
                    }

                    break;
                case ConnectionState.Failed:
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
            if (State == ChannelState.Attached)
            {
                try
                {
                    callback?.Invoke(true, null);
                }
                catch (Exception e)
                {
                    Logger.Error("Error in attach callback", e);
                }

                return;
            }

            if (IsTerminalConnectionState)
            {
                throw new AblyException($"Cannot attach when connection is in {ConnectionState} state");
            }

            if (AttachedAwaiter.StartWait(callback, ConnectionManager.Options.RealtimeRequestTimeout))
            {
                SetChannelState(ChannelState.Attaching);
            }
        }

        public Task<Result> AttachAsync()
        {
            return TaskWrapper.Wrap(Attach);
        }

        private void OnAttachTimeout()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"#{Name} didn't Attach within {ConnectionManager.Options.RealtimeRequestTimeout}. Setting state back to {PreviousState}");
            }

            SetChannelState(PreviousState, new ErrorInfo("Channel didn't attach within the default timeout", 50000));
        }

        private void OnDetachTimeout()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"#{Name} didn't Detach within {ConnectionManager.Options.RealtimeRequestTimeout}. Setting state back to {PreviousState}");
            }

            SetChannelState(PreviousState, new ErrorInfo("Channel didn't detach within the default timeout", 50000));
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
                try
                {
                    callback?.Invoke(true, null);
                }
                catch (Exception e)
                {
                    Logger.Error("Error in detach callback", e);
                }

                return;
            }

            if (State == ChannelState.Failed)
            {
                throw new AblyException("Cannot Detach channel because it is in Failed state.");
            }

            if (DetachedAwaiter.StartWait(callback, ConnectionManager.Options.RealtimeRequestTimeout))
            {
                SetChannelState(ChannelState.Detaching);
            }
        }

        public Task<Result> DetachAsync()
        {
            return TaskWrapper.Wrap(Detach);
        }

        public void Subscribe(Action<Message> handler)
        {
            if (State != ChannelState.Attached && State != ChannelState.Attaching)
            {
                Attach();
            }

            _handlers.Add(new MessageHandlerAction<Message>(handler));
        }

        public void Subscribe(string eventName, Action<Message> handler)
        {
            if (State != ChannelState.Attached && State != ChannelState.Attaching)
            {
                Attach();
            }

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
            Publish(new[] { message }, callback);
        }

        public Task<Result> PublishAsync(Message message)
        {
            return PublishAsync(new[] { message });
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
            var query = new PaginatedRequestParams();
            if (untilAttach)
            {
                AddUntilAttachParameter(query);
            }

            return RestChannel.HistoryAsync(query);
        }

        public Task<PaginatedResult<Message>> HistoryAsync(PaginatedRequestParams query, bool untilAttach = false)
        {
            query = query ?? new PaginatedRequestParams();
            if (untilAttach)
            {
                AddUntilAttachParameter(query);
            }

            return RestChannel.HistoryAsync(query);
        }

        public void OnError(ErrorInfo error)
        {
            ErrorReason = error; // Set or clear the error

            RealtimeClient.NotifyExternalClients(() => Error.Invoke(this, new ChannelErrorEventArgs(error)));
        }

        public void Dispose()
        {
            AttachedAwaiter?.Dispose();
            DetachedAwaiter?.Dispose();
            _handlers.RemoveAll();
            Presence?.Dispose();
            UnSubscribeFromConnectionEvents();
        }

        internal void AddUntilAttachParameter(PaginatedRequestParams query)
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
                if (State == ChannelState.Initialized)
                {
                    Attach();
                }

                // Not connected, queue the message
                lock (_lockQueue)
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"#{Name}:{State} queuing message");
                    }

                    QueuedMessages.Add(new MessageAndCallback(msg, callback));
                    return;
                }
            }

            if (State == ChannelState.Attached)
            {
                SendMessage(msg, callback);
                return;
            }

            // Invalid state, throw
            throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000, HttpStatusCode.BadRequest));
        }

        internal void SetChannelState(ChannelState state, ProtocolMessage protocolMessage)
        {
            SetChannelState(state, protocolMessage.Error, protocolMessage);
        }

        internal void SetChannelState(ChannelState state, bool emitUpdate)
        {
            SetChannelState(state, null, null, emitUpdate);
        }

        internal void SetChannelState(ChannelState state, ErrorInfo error = null, ProtocolMessage protocolMessage = null, bool emitUpdate = false)
        {
            if (Logger.IsDebug)
            {
                var errorMessage = error != null ? "Error: " + error : string.Empty;
                Logger.Debug($"#{Name}: Changing state to: '{state}'. {errorMessage}");
            }

            OnError(error);

            // never emit a ChannelState ChannelEvent for a state equal to the previous state (RTL2g)
            if (!emitUpdate && State == state)
            {
                Logger.Debug($"#{Name}: Duplicate state '{state}' received, not updating.");
                return;
            }

            ChannelEvent channelEvent;
            if (emitUpdate)
            {
                channelEvent = ChannelEvent.Update;
            }
            else
            {
                channelEvent = (ChannelEvent) state;
            }

            var channelStateChange = new ChannelStateChange(state, State, error, protocolMessage);
            HandleStateChange(state, error, protocolMessage);
            InternalStateChanged.Invoke(this, channelStateChange);

            // Notify external client using the thread they subscribe on
            RealtimeClient.NotifyExternalClients(() =>
            {
                try
                {
                    StateChanged.Invoke(this, channelStateChange);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error notifying event handlers for state change: {state}", ex);
                }

                Emit(channelEvent, channelStateChange);
            });
        }

        private void HandleStateChange(ChannelState state, ErrorInfo error, ProtocolMessage protocolMessage)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"HandleStateChange state change from {State} to {state}");
            }

            var oldState = State;
            State = state;

            switch (state)
            {
                case ChannelState.Attaching:
                    DetachedAwaiter.Fail(new ErrorInfo("Channel transitioned to Attaching", 50000));

                    if (ConnectionState == ConnectionState.Initialized)
                    {
                        Connection.Connect();
                    }

                    if (IsTerminalConnectionState)
                    {
                        Logger.Warning($"#{Name}. Cannot send Attach messages when connection is in {ConnectionState} State");
                    }
                    else
                    {
                        SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, Name));
                    }

                    break;
                case ChannelState.Attached:
                    if (protocolMessage != null)
                    {
                        if (protocolMessage.HasFlag(ProtocolMessage.Flag.HasPresence))
                        {
                            if (Logger.IsDebug)
                            {
                                Logger.Debug($"Protocol message has presence flag. Starting Presence SYNC. Flag: {protocolMessage.Flags}");
                            }

                            Presence.StartSync();
                        }
                        else
                        {
                            /* RTP1 If [HAS_PRESENCE] flag is 0 or there is no flags field,
                             * the presence map should be considered in sync immediately
                             * with no members present on the channel */
                    Presence.SkipSync();
                        }

                        AttachedSerial = protocolMessage.ChannelSerial;
                    }

                    if (IsTerminalConnectionState == false)
                    {
                        SendQueuedMessages();
                    }

                    break;
                case ChannelState.Detaching:
                    AttachedAwaiter.Fail(new ErrorInfo("Channel transitioned to detaching", 50000));

                    if (ConnectionState == ConnectionState.Closed || ConnectionState == ConnectionState.Connecting ||
                        ConnectionState == ConnectionState.Suspended)
                    {
                        SetChannelState(ChannelState.Detached, error);
                    }
                    else if (ConnectionState != ConnectionState.Failed)
                    {
                        SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Detach, Name));
                    }
                    else
                    {
                        Logger.Warning($"#{Name}. Cannot send Detach messages when connection is in Failed State");
                    }

                    break;
                case ChannelState.Detached:
                    /* RTL13a check for unexpected detach */
                    switch (oldState)
                    {
                        /* (RTL13a) If the channel is in the @ATTACHED@ or @SUSPENDED@ states,
                         an attempt to reattach the channel should be made immediately */
                        case ChannelState.Attached:
                        case ChannelState.Suspended:
                            try
                            {
                                Attach();
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Attempting reattach threw exception", e);
                                SetChannelState(oldState, protocolMessage);
                            }
                            break;
                        case ChannelState.Attaching:
                            /* RTL13b says we need to be suspended, but continue to retry */
                            Logger.Debug($"Server initiated detach for channel {Name} whilst attaching; moving to suspended");
                            SetChannelState(oldState, protocolMessage);
                            ReattachAfterTimeout();
                            break;
                        default:
                            ClearAndFailChannelQueuedMessages(error);
                            break;
                    }
                    break;
                case ChannelState.Failed:
                    AttachedAwaiter.Fail(error);
                    DetachedAwaiter.Fail(error);
                    ClearAndFailChannelQueuedMessages(error);
                    break;
                case ChannelState.Suspended:
                    ClearAndFailChannelQueuedMessages(error);
                    break;
            }
        }

        private void ReattachAfterTimeout()
        {
            Task.Run(async () =>
            {
                await Task.Delay(RealtimeClient.Options.SuspendedRetryTimeout);
                try
                {
                    Attach();
                }
                catch (Exception e)
                {
                    Logger.Error("Reattach channel failed; channel = " + Name, e);
                }
            }).ConfigureAwait(false);
        }

        private void ClearAndFailChannelQueuedMessages(ErrorInfo error)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Clearing channel #{Name} queued messages. Count: " + QueuedMessages.Count);
            }

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
            var channelHandlers = _handlers.GetHandlers().ToList();
            if (Logger.IsDebug)
            {
                Logger.Debug($"Notifying {channelHandlers.Count} handlers in #{Name} channel");
            }

            foreach (var handler in channelHandlers)
            {
                var loopHandler = handler;
                RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message, Logger));
            }

            if (message.Name.IsNotEmpty())
            {
                var namedHandlers = _handlers.GetHandlers(message.Name).ToList();
                if (Logger.IsDebug)
                {
                    Logger.Debug($"Notifying {namedHandlers.Count} handlers for messages {message.Name} in #{Name} channel");
                }

                foreach (var specificHandler in namedHandlers)
                {
                    var loopHandler = specificHandler;
                    RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message, Logger));
                }
            }
        }

        private bool IsTerminalConnectionState => ConnectionState == ConnectionState.Closed ||
                                                  ConnectionState == ConnectionState.Closing ||
                                                  ConnectionState == ConnectionState.Failed;

        private int SendQueuedMessages()
        {
            List<MessageAndCallback> list;
            lock (_lockQueue)
            {
                if (QueuedMessages.Count <= 0)
                {
                    return 0;
                }

                // Swap the list.
                list = new List<MessageAndCallback>(QueuedMessages);
                QueuedMessages.Clear();
            }

            foreach (var qpm in list)
            {
                SendMessage(qpm.Message, qpm.Callback);
            }

            return list.Count;
        }

        private void SendMessage(ProtocolMessage protocolMessage, Action<bool, ErrorInfo> callback = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"RealtimeChannel.SendMessage:{protocolMessage.Action}");
            }

            ConnectionManager.Send(protocolMessage, callback, Options);
        }

        /// <summary>
        /// Emits an UPDATE if the channel is ATTACHED
        /// </summary>
        /// <param name="errorInfo"></param>
        /// <param name="resumed"></param>
        internal void EmitUpdate(ErrorInfo errorInfo, bool resumed)
        {
            if (State == ChannelState.Attached)
            {
                Emit(ChannelEvent.Update, new ChannelStateChange(State, State, errorInfo, resumed));
            }
        }

        /// <summary>
        /// Emits an <see cref="ChannelEvent.Update"/> if the current channel state is <see cref="ChannelState.Attached"/>
        /// </summary>
        /// <param name="protocolMessage"></param>
        internal void EmitUpdate(ChannelState state, ProtocolMessage protocolMessage)
        {
            SetChannelState(State, protocolMessage.Error, protocolMessage, emitUpdate: true);
        }
    }
}
