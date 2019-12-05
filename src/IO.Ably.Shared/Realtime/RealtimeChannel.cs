using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IO.Ably.Rest;
using IO.Ably.Transport;
using IO.Ably.Types;
using IO.Ably.Utils;
using Newtonsoft.Json.Linq;
using IO.Ably.MessageEncoders;

namespace IO.Ably.Realtime
{
    internal class RealtimeChannel : EventEmitter<ChannelEvent, ChannelStateChange>, IRealtimeChannel
    {
        private readonly Handlers<Message> _handlers = new Handlers<Message>();
        private ChannelOptions _options;
        private ChannelState _state;

        internal EncodingDecodingContext EncodingDecodingContext { get; private set; }

        public event EventHandler<ChannelStateChange> StateChanged = delegate { };

        internal event EventHandler<ChannelStateChange> InternalStateChanged = delegate { };

        public event EventHandler<ChannelErrorEventArgs> Error = delegate { };

        internal AblyRealtime RealtimeClient { get; }

        private string PreviousConnectionId { get; set; }

        private ConnectionState ConnectionState => Connection.State;

        private IConnectionManager ConnectionManager => RealtimeClient.ConnectionManager;

        private Connection Connection => RealtimeClient.Connection;

        internal IRestChannel RestChannel => RealtimeClient.RestClient.Channels.Get(Name);

        internal ChannelAwaiter AttachedAwaiter { get; }

        internal ChannelAwaiter DetachedAwaiter { get; }

        protected override Action<Action> NotifyClient => RealtimeClient.NotifyExternalClients;

        public ErrorInfo ErrorReason { get; internal set; }

        public ChannelProperties Properties { get; } = new ChannelProperties();

        public ChannelOptions Options
        {
            get => _options;
            set => _options = value ?? new ChannelOptions();
        }

        public string Name { get; }

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

        internal RealtimeChannel(
            string name,
            string clientId,
            AblyRealtime realtimeClient,
            ChannelOptions options = null)
            : base(options?.Logger)
        {
            Name = name;
            Options = options;
            EncodingDecodingContext = new EncodingDecodingContext(options);
            Presence = new Presence(realtimeClient.ConnectionManager, this, clientId, Logger);
            RealtimeClient = realtimeClient;
            State = ChannelState.Initialized;
            AttachedAwaiter = new ChannelAwaiter(this, ChannelState.Attached, Logger, OnAttachTimeout);
            DetachedAwaiter = new ChannelAwaiter(this, ChannelState.Detached, Logger, OnDetachTimeout);
        }

        internal void ConnectionStateChanged(ConnectionStateChange connectionStateChange)
        {
            var connectionRefreshed = PreviousConnectionId != Connection.Id;
            if (connectionRefreshed)
            {
                PreviousConnectionId = Connection.Id;
            }

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

                    /*
                     * Connection state is only maintained server-side for a brief period,
                     * given by the connectionStateTtl in the connectionDetails (2 minutes at time of writing, see CD2f).
                     * If a client has been disconnected for longer than the connectionStateTtl
                     * it should clear the local connection state and any connection attempts should be made as for a fresh connection
                     *
                     * (RTN15g3) When a connection attempt succeeds after the connection state has been cleared in this way,
                     * channels that were previously ATTACHED, ATTACHING, or SUSPENDED must be automatically reattached,
                     * just as if the connection was a resume attempt which failed per RTN15c3
                     *
                     * Given the above, if the channel is ATTACHED and the connection is fresh
                     * then set the channel to ATTACHING to trigger an ATTACH attempt
                     */
                    if (State == ChannelState.Attached && connectionRefreshed)
                    {
                        if (AttachedAwaiter.StartWait(null, ConnectionManager.Options.RealtimeRequestTimeout, restart: true))
                        {
                            SetChannelState(ChannelState.Attaching, false);
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

        public void Attach(Action<bool, ErrorInfo> callback = null)
        {
            if (State == ChannelState.Attached)
            {
                ActionUtils.SafeExecute(() => callback?.Invoke(true, null), Logger, $"{Name}-Attach()");

                return;
            }

            if (IsTerminalConnectionState)
            {
                throw new AblyException($"Cannot attach when connection is in {ConnectionState} state");
            }

            Attach(null, null, callback);
        }

        private void Attach(ErrorInfo error, ProtocolMessage msg = null, Action<bool, ErrorInfo> callback = null)
        {
            var actualError = error == null && msg?.Error != null ? msg.Error : error;
            if (AttachedAwaiter.StartWait(callback, ConnectionManager.Options.RealtimeRequestTimeout))
            {
                SetChannelState(ChannelState.Attaching, error, msg);
            }
        }

        public async Task<Result> AttachAsync()
        {
            return await TaskWrapper.Wrap(Attach);
        }

        private void OnAttachTimeout()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"#{Name} didn't Attach within {ConnectionManager.Options.RealtimeRequestTimeout}. Setting state to {ChannelState.Suspended}");
            }

            // RTL4f
            SetChannelState(ChannelState.Suspended, new ErrorInfo($"Channel didn't attach within  {ConnectionManager.Options.RealtimeRequestTimeout}", 90007, HttpStatusCode.RequestTimeout));
        }

        private void OnDetachTimeout()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"#{Name} didn't Detach within {ConnectionManager.Options.RealtimeRequestTimeout}. Setting state back to {PreviousState}");
            }

            SetChannelState(PreviousState, new ErrorInfo("Channel didn't detach within the default timeout", 50000));
        }

        public void Detach(Action<bool, ErrorInfo> callback = null)
        {
            if (State == ChannelState.Initialized || State == ChannelState.Detaching ||
                State == ChannelState.Detached)
            {
                ActionUtils.SafeExecute(() => callback?.Invoke(true, null), Logger, $"{Name}-Detach()");
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
            else
            {
                Logger.Debug("Detach is already happening. Channel: " + Name);
            }
        }

        public async Task<Result> DetachAsync()
        {
            return await TaskWrapper.Wrap(Detach);
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

            var failResult = Result.Fail(new ErrorInfo("PublishAsync timeout expired. Message was not confirmed by the server"));
            return await tw.Task.TimeoutAfter(RealtimeClient.Options.RealtimeRequestTimeout, failResult);
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

            if (error != null)
            {
                RealtimeClient.NotifyExternalClients(() => Error.Invoke(this, new ChannelErrorEventArgs(error)));
            }
        }

        internal void RemoveAllListeners()
        {
            AttachedAwaiter?.Dispose();
            DetachedAwaiter?.Dispose();
            _handlers.RemoveAll();
            Presence?.RemoveAllListeners();
        }

        internal void AddUntilAttachParameter(PaginatedRequestParams query)
        {
            if (State != ChannelState.Attached)
            {
                throw new AblyException("Channel is not attached. Cannot use untilAttach parameter");
            }

            query.ExtraParameters.Add("fromSerial", Properties.AttachSerial);
        }

        private void PublishImpl(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback)
        {
            if (State == ChannelState.Suspended || State == ChannelState.Failed)
            {
                throw new AblyException(new ErrorInfo($"Unable to publish in {State} state", 40000, HttpStatusCode.BadRequest));
            }

            if (!Connection.CanPublishMessages)
            {
                throw new AblyException(new ErrorInfo($"Message cannot be published. Client is not allowed to queue messages when connection is in {State} state", 40000, HttpStatusCode.BadRequest));
            }

            var msg = new ProtocolMessage(ProtocolMessage.MessageAction.Message, Name)
            {
                Messages = messages.ToArray(),
            };

            SendMessage(msg, callback);
        }

        internal void SetChannelState(ChannelState state, ProtocolMessage protocolMessage)
        {
            SetChannelState(state, protocolMessage?.Error, protocolMessage);
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
                channelEvent = (ChannelEvent)state;
            }

            var channelStateChange = new ChannelStateChange(channelEvent, state, State, error, protocolMessage);
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
                case ChannelState.Attached:
                    Presence.ChannelAttached(protocolMessage);
                    break;
                case ChannelState.Detached:
                    /* RTL13a check for unexpected detach */
                    switch (oldState)
                    {
                        /* (RTL13a) If the channel is in the @ATTACHED@ or @SUSPENDED@ states,
                         an attempt to reattach the channel should be made immediately */
                        case ChannelState.Attached:
                        case ChannelState.Suspended:
                            SetChannelState(ChannelState.Detached, error, protocolMessage);
                            Reattach(error, protocolMessage);
                            break;
                        case ChannelState.Attaching:
                            /* RTL13b says we need to become suspended, but continue to retry */
                            Logger.Debug($"Server initiated detach for channel {Name} whilst attaching; moving to suspended");
                            SetChannelState(ChannelState.Suspended, error, protocolMessage);
                            ReattachAfterTimeout(error, protocolMessage);
                            break;
                        default:
                            break;
                    }

                    Presence.ChannelDetachedOrFailed(error);

                    break;
                case ChannelState.Failed:
                    AttachedAwaiter.Fail(error);
                    DetachedAwaiter.Fail(error);
                    Presence.ChannelDetachedOrFailed(error);
                    break;
                case ChannelState.Suspended:
                    Presence.ChannelSuspended(error);
                    break;
            }
        }

        private void Reattach(ErrorInfo error, ProtocolMessage msg)
        {
            TaskUtils.RunInBackground(
                () =>
            {
                try
                {
                    Attach(error, msg, (success, info) =>
                    {
                        if (!success)
                        {
                            // If the attach timed out the channel will set SUSPENDED (as described by RTL4f and RTL13b)
                            if (State == ChannelState.Suspended)
                            {
                                ReattachAfterTimeout(error, msg);
                            }
                        }
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Reattach channel failed; channel = " + Name, e);
                }
            },
                e => Logger.Warning(e.Message));
        }

        /// <summary>
        /// should only be called when the channel is SUSPENDED.
        /// </summary>
        private void ReattachAfterTimeout(ErrorInfo error, ProtocolMessage msg)
        {
            Task.Run(async () =>
            {
                await Task.Delay(RealtimeClient.Options.ChannelRetryTimeout);

                // only retry if the connection is connected (RTL13c)
                if (Connection.State == ConnectionState.Connected)
                {
                    Reattach(error, msg);
                }
            }).ConfigureAwait(false);
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

        private void SendMessage(ProtocolMessage protocolMessage, Action<bool, ErrorInfo> callback = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"RealtimeChannel.SendMessage:{protocolMessage.Action}");
            }

            ConnectionManager.Send(protocolMessage, callback, Options);
        }

        internal void EmitUpdate(ErrorInfo errorInfo, bool resumed)
        {
            if (State == ChannelState.Attached)
            {
                Emit(ChannelEvent.Update, new ChannelStateChange(ChannelEvent.Update, State, State, errorInfo, resumed));
            }
        }

        public JObject GetCurrentState()
        {
            var state = new JObject();
            state["name"] = Name;
            state["options"] = JObject.FromObject(_options);
            state["state"] = JToken.FromObject(_state);
            state["timers"] = JObject.FromObject(new
            {
                awaitTimer = AttachedAwaiter.Waiting,
                detachTimer = DetachedAwaiter.Waiting,
            });
            state["emitters"] = GetState();
            state["handlers"] = _handlers.GetState();
            state["presence"] = Presence.GetState();
            return state;
        }
    }
}
