using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Types;
using IO.Ably.Utils;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Realtime
{
    /// <summary>
    /// A class that provides access to presence operations and state for the associated Channel.
    /// </summary>
    public sealed partial class Presence : IDisposable
    {
        private readonly RealtimeChannel _channel;
        private readonly string _clientId;
        private readonly Handlers<PresenceMessage> _handlers = new Handlers<PresenceMessage>();
        private readonly IConnectionManager _connection;
        private string _currentSyncChannelSerial;
        private bool _initialSyncCompleted;
        private bool _disposedValue;

        internal Presence(IConnectionManager connection, RealtimeChannel channel, string clientId, ILogger logger)
        {
            Logger = logger;
            Map = new PresenceMap(channel.Name, logger);
            InternalMap = new PresenceMap(channel.Name, logger);
            PendingPresenceQueue = new ConcurrentQueue<QueuedPresenceMessage>();
            _connection = connection;
            _channel = channel;
            _clientId = clientId;

            SetUpAutoPresenceEnterOnChannelAttach();
        }

        // RTP17f
        internal void SetUpAutoPresenceEnterOnChannelAttach()
        {
            _channel.StateChanged += (_, change) =>
            {
                if (change.Current == ChannelState.Attached && change.Previous != ChannelState.Attached)
                {
                    EnterPresenceForRecordedMembersWithCurrentConnectionId();
                }
            };
        }

        private event EventHandler InitialSyncCompleted;

        internal event EventHandler SyncCompleted;

        internal ILogger Logger { get; private set; }

        /// <summary>
        /// Has the sync completed.
        /// </summary>
        public bool SyncComplete
        {
            get => Map.InitialSyncCompleted | _initialSyncCompleted;

            private set
            {
                _initialSyncCompleted = value;
                if (_initialSyncCompleted)
                {
                    OnInitialSyncCompleted();
                }
            }
        }

        /// <summary>
        /// Indicates whether there is currently a sync in progress.
        /// </summary>
        public bool IsSyncInProgress => Map.IsSyncInProgress;

        internal bool InternalSyncComplete => !Map.IsSyncInProgress && SyncComplete;

        internal PresenceMap Map { get; }

        internal PresenceMap InternalMap { get; } // RTP17

        internal ConcurrentQueue<QueuedPresenceMessage> PendingPresenceQueue { get; }

        /// <summary>
        /// Called when a protocol message HasPresenceFlag == false. The presence map should be considered in sync immediately
        /// with no members present on the channel. See [RTP1] for more detail.
        /// </summary>
        internal void SkipSync()
        {
            SyncComplete = true;
        }

        /// <summary>
        /// Disposes the current Presence instance. Removes all listening handlers.
        /// </summary>
        internal void RemoveAllListeners()
        {
            InitialSyncCompleted = null;
            SyncCompleted = null;
            _handlers.RemoveAll();
        }

        /// <summary>
        ///     Get the presence state given a set of options <see cref="GetParams"/>. Implicitly attaches the Channel.
        ///     However, if the channel is in or moves to the FAILED.
        ///     state before the operation succeeds, it will result in an error.
        /// </summary>
        /// <param name="options">Options for the GetAsync. For details <see cref="GetParams"/>.</param>
        /// <returns>a list of PresenceMessages.</returns>
        public async Task<IEnumerable<PresenceMessage>> GetAsync(GetParams options)
        {
            // RTP11b
            if (_channel.State == ChannelState.Failed || _channel.State == ChannelState.Detached)
            {
                throw new AblyException(new ErrorInfo($"channel operation failed. Invalid channel state ({_channel.State})", ErrorCodes.ChannelOperationFailedWithInvalidState));
            }

            if (_channel.State == ChannelState.Initialized)
            {
                _channel.Attach();
            }

            var getOptions = options ?? new GetParams();

            if (getOptions.WaitForSync)
            {
                // RTP11d
                if (_channel.State == ChannelState.Suspended)
                {
                    throw new AblyException(new ErrorInfo($"Channel {_channel.Name}: presence state is out of sync due to the channel being in a SUSPENDED state", 91005));
                }

                _ = await WaitForSyncAsync();
            }

            var result = Map.Values.Where(x => (getOptions.ClientId.IsEmpty() || x.ClientId == getOptions.ClientId)
                                               && (getOptions.ConnectionId.IsEmpty() || x.ConnectionId == getOptions.ConnectionId));
            return result;
        }

        /// <summary>
        ///     Get the presence state for the current channel, optionally waiting for Sync to complete.
        ///     Implicitly attaches the Channel. However, if the channel is in or moves to the FAILED.
        ///     state before the operation succeeds, it will result in an error.
        /// </summary>
        /// <param name="waitForSync">whether it should wait for a sync to complete.</param>
        /// <returns>the current present members.</returns>
        public async Task<IEnumerable<PresenceMessage>> GetAsync(bool waitForSync)
        {
            return await GetAsync(new GetParams { WaitForSync = waitForSync });
        }

        /// <summary>
        ///     Get the presence state for a given clientId, optionally waiting for Sync to complete.
        ///     Implicitly attaches the Channel. However, if the channel is in or moves to the FAILED.
        ///     state before the operation succeeds, it will result in an error.
        /// </summary>
        /// <param name="clientId">requests Presence for the this clientId.</param>
        /// <param name="waitForSync">whether it should wait for a sync to complete.</param>
        /// <returns>the current present members.</returns>
        public async Task<IEnumerable<PresenceMessage>> GetAsync(string clientId, bool waitForSync)
        {
            return await GetAsync(new GetParams { ClientId = clientId, WaitForSync = waitForSync });
        }

        /// <summary>
        ///     Get the presence state for a given clientId and connectionId, optionally waiting for Sync to complete.
        ///     Implicitly attaches the Channel. However, if the channel is in or moves to the FAILED.
        ///     state before the operation succeeds, it will result in an error.
        /// </summary>
        /// <param name="clientId">requests Presence for the this clientId.</param>
        /// <param name="connectionId">requests Presence for the a specific connectionId.</param>
        /// <param name="waitForSync">whether it should wait for a sync to complete.</param>
        /// <returns>the current present members.</returns>
        public async Task<IEnumerable<PresenceMessage>> GetAsync(string clientId = null, string connectionId = null, bool waitForSync = true)
        {
            return await GetAsync(new GetParams { ClientId = clientId, ConnectionId = connectionId, WaitForSync = waitForSync });
        }

        private async Task<bool> WaitForSyncAsync()
        {
            var tsc = new TaskCompletionSource<bool>();

            // The InternalSync should be completed and the channels Attached or Attaching
            void CheckAndSet()
            {
                if (InternalSyncComplete
                    && (_channel.State == ChannelState.Attached || _channel.State == ChannelState.Attaching))
                {
                    tsc.TrySetResult(true);
                }
            }

            // if the channel state changes and is not Attached or Attaching then we should exit
            void OnChannelStateChanged(object sender, ChannelStateChange args)
            {
                if (_channel.State != ChannelState.Attached && _channel.State != ChannelState.Attaching)
                {
                    tsc.TrySetResult(false);
                }
            }

            void OnSyncEvent(object sender, EventArgs args) => CheckAndSet();

            _channel.StateChanged += OnChannelStateChanged;
            InitialSyncCompleted += OnSyncEvent;
            Map.SyncNoLongerInProgress += OnSyncEvent;

            // Do a manual check in case we are already in the desired state
            CheckAndSet();
            bool syncIsComplete = await tsc.Task;

            // unsubscribe from events
            _channel.StateChanged -= OnChannelStateChanged;
            InitialSyncCompleted -= OnSyncEvent;
            Map.SyncNoLongerInProgress -= OnSyncEvent;

            if (!syncIsComplete)
            {
                /* invalid channel state */
                int errorCode;
                string errorMessage;
                if (_channel.State == ChannelState.Suspended)
                {
                    /* (RTP11d) If the Channel is in the SUSPENDED state then the get function will by default,
                    * or if waitForSync is set to true, result in an error with code 91005 and a message stating
                    * that the presence state is out of sync due to the channel being in a SUSPENDED state */
                    errorCode = 91005;
                    errorMessage = $"Channel {_channel.Name}: presence state is out of sync due to the channel being in a SUSPENDED state";
                }
                else
                {
                    /* RTP11b */
                    errorCode = ErrorCodes.ChannelOperationFailedWithInvalidState;
                    errorMessage = $"Channel {_channel.Name}: cannot get presence state when the channel is in a {_channel.State} state.";
                }

                Logger.Debug($"{errorMessage} (Error Code: {errorCode})");
                throw new AblyException(new ErrorInfo(errorMessage, errorCode));
            }

            return tsc.Task.Result;
        }

        /// <summary>
        /// Subscribe to presence events on the associated Channel. This implicitly
        /// attaches the Channel if it is not already attached.
        /// </summary>
        /// <param name="handler">handler to be notified for the arrival of presence messages.</param>
        public void Subscribe(Action<PresenceMessage> handler)
        {
            if (_channel.State != ChannelState.Attached && _channel.State != ChannelState.Attaching)
            {
                _channel.Attach();
            }

            _handlers.Add(handler.ToHandlerAction());
        }

        /// <summary>
        /// Subscribe to presence events with a specific action on the associated Channel. This implicitly
        /// attaches the Channel if it is not already attached.
        /// </summary>
        /// <param name="action">action to be observed.</param>
        /// <param name="handler">handler to be notified for the arrival of presence messages.</param>
        public void Subscribe(PresenceAction action, Action<PresenceMessage> handler)
        {
            if ((_channel.State != ChannelState.Attached) && (_channel.State != ChannelState.Attaching))
            {
                _channel.Attach();
            }

            _handlers.Add(action.ToString(), new MessageHandlerAction<PresenceMessage>(handler));
        }

        /// <summary>
        /// Subscribe to presence events with a specific action on the associated Channel. This implicitly
        /// attaches the Channel if it is not already attached.
        /// </summary>
        /// <param name="action">action to be observed.</param>
        /// <param name="handler">handler to be notified for the arrival of presence messages.</param>
        public void Subscribe(PresenceAction action, Func<PresenceMessage, Task> handler)
        {
            Subscribe(action, message => { _ = handler(message); });
        }

        /// <summary>
        /// Unsubscribe a previously subscribed handler.
        /// </summary>
        /// <param name="handler">the handler to be unsubscribed.</param>
        /// <returns>true if unsubscribed, false if the handler doesn't exist.</returns>
        public bool Unsubscribe(Action<PresenceMessage> handler)
        {
            return _handlers.Remove(handler.ToHandlerAction());
        }

        /// <summary>
        /// Unsubscribes all attached handlers.
        /// </summary>
        public void Unsubscribe()
        {
            _handlers.RemoveAll();
        }

        /// <summary>
        /// Unsubscribe a specific handler for a specific action.
        /// </summary>
        /// <param name="presenceAction">the specific action.</param>
        /// <param name="handler">the handler to be unsubscribed.</param>
        /// <returns>true if unsubscribed, false if the handler is not found.</returns>
        public bool Unsubscribe(PresenceAction presenceAction, Action<PresenceMessage> handler)
        {
            return _handlers.Remove(presenceAction.ToString(), handler.ToHandlerAction());
        }

        /// <summary>
        /// Enter this client into this channel. This client will be added to the presence set
        /// and presence subscribers will see an enter message for this client.
        /// </summary>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <param name="callback">a listener to be notified on completion of the operation.</param>
        public void Enter(object data = null, Action<bool, ErrorInfo> callback = null)
        {
            EnterClient(_clientId, data, callback);
        }

        /// <summary>
        /// Enter this client into this channel. This client will be added to the presence set
        /// and presence subscribers will see an enter message for this client.
        /// </summary>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <returns>Result whether the operation was success or error.</returns>
        public Task<Result> EnterAsync(object data = null)
        {
            return EnterClientAsync(_clientId, data);
        }

        /// <summary>
        /// Update the presence data for this client. If the client is not already a member of
        /// the presence set it will be added, and presence subscribers will see an enter or
        /// update message for this client.
        /// </summary>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <param name="callback">a listener to be notified on completion of the operation.</param>
        public void Update(object data = null, Action<bool, ErrorInfo> callback = null)
        {
            UpdateClient(_clientId, data, callback);
        }

        /// <summary>
        /// Update the presence data for this client. If the client is not already a member of
        /// the presence set it will be added, and presence subscribers will see an enter or
        /// update message for this client.
        /// </summary>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <returns>Result whether the operation was success or error.</returns>
        public Task<Result> UpdateAsync(object data = null)
        {
            return UpdateClientAsync(_clientId, data);
        }

        /// <summary>
        ///  Update the presence data for a specified client into this channel.
        ///  If the client is not already a member of the presence set it will be added,
        ///  and presence subscribers will see a corresponding presence message
        ///  with an empty data payload.As for #enterClient above, the connection
        ///  must be authenticated in a way that enables it to represent an arbitrary clientId.
        /// </summary>
        /// <param name="clientId">the id of the client.</param>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <param name="callback">a listener to be notified on completion of the operation.</param>
        public void UpdateClient(string clientId, object data, Action<bool, ErrorInfo> callback = null)
        {
            UpdatePresence(new PresenceMessage(PresenceAction.Update, clientId, data), callback);
        }

        /// <summary>
        ///  Update the presence data for a specified client into this channel.
        ///  If the client is not already a member of the presence set it will be added,
        ///  and presence subscribers will see a corresponding presence message
        ///  with an empty data payload.As for #enterClient above, the connection
        ///  must be authenticated in a way that enables it to represent an arbitrary clientId.
        /// </summary>
        /// <param name="clientId">the id of the client.</param>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <returns>Result whether the operation was success or error.</returns>
        public Task<Result> UpdateClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Update, clientId, data));
        }

        /// <summary>
        /// Enter a specified client into this channel.The given clientId will be added to
        /// the presence set and presence subscribers will see a corresponding presence message
        /// with an empty data payload.
        /// This method is provided to support connections (eg connections from application
        /// server instances) that act on behalf of multiple clientIds. In order to be able to
        /// enter the channel with this method, the client library must have been instanced
        /// either with a key, or with a token bound to the wildcard clientId.
        /// </summary>
        /// <param name="clientId">id of the client.</param>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <param name="callback">a listener to be notified on completion of the operation.</param>
        public void EnterClient(string clientId, object data, Action<bool, ErrorInfo> callback = null)
        {
            UpdatePresence(new PresenceMessage(PresenceAction.Enter, clientId, data), callback);
        }

        /// <summary>
        /// Enter a specified client into this channel.The given clientId will be added to
        /// the presence set and presence subscribers will see a corresponding presence message
        /// with an empty data payload.
        /// This method is provided to support connections (eg connections from application
        /// server instances) that act on behalf of multiple clientIds. In order to be able to
        /// enter the channel with this method, the client library must have been instanced
        /// either with a key, or with a token bound to the wildcard clientId.
        /// </summary>
        /// /// <param name="clientId">id of the client.</param>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <returns>Result whether the operation was success or error.</returns>
        public Task<Result> EnterClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Enter, clientId, data));
        }

        /// <summary>
        /// Leave this client from this channel. This client will be removed from the presence
        /// set and presence subscribers will see a leave message for this client.
        /// </summary>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <param name="callback">a listener to be notified on completion of the operation.</param>
        public void Leave(object data = null, Action<bool, ErrorInfo> callback = null)
        {
            LeaveClient(_clientId, data, callback);
        }

        /// <summary>
        /// Leave this client from this channel. This client will be removed from the presence
        /// set and presence subscribers will see a leave message for this client.
        /// </summary>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <returns>Result whether the operation was success or error.</returns>
        public Task<Result> LeaveAsync(object data = null)
        {
            return LeaveClientAsync(_clientId, data);
        }

        /// <summary>
        /// Leave a given client from this channel. This client will be removed from the
        /// presence set and presence subscribers will see a corresponding presence message
        /// with an empty data payload.
        /// This method is provided to support connections (eg connections from application
        /// server instances) that act on behalf of multiple clientIds. In order to be able to
        /// enter the channel with this method, the client library must have been instanced
        /// either with a key, or with a token bound to the wildcard clientId.
        /// </summary>
        /// <param name="clientId">the id of the client.</param>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <param name="callback">a listener to be notified on completion of the operation.</param>
        public void LeaveClient(string clientId, object data, Action<bool, ErrorInfo> callback = null)
        {
            UpdatePresence(new PresenceMessage(PresenceAction.Leave, clientId, data), callback);
        }

        /// <summary>
        /// Leave a given client from this channel. This client will be removed from the
        /// presence set and presence subscribers will see a corresponding presence message
        /// with an empty data payload.
        /// This method is provided to support connections (eg connections from application
        /// server instances) that act on behalf of multiple clientIds. In order to be able to
        /// enter the channel with this method, the client library must have been instanced
        /// either with a key, or with a token bound to the wildcard clientId.
        /// </summary>
        /// <param name="clientId">the id of the client.</param>
        /// <param name="data">optional data (eg a status message) for this member.</param>
        /// <returns>Result whether the operation was success or error.</returns>
        public Task<Result> LeaveClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Leave, clientId, data));
        }

        internal async Task<Result> UpdatePresenceAsync(PresenceMessage msg)
        {
            return await TaskWrapper.Wrap(callback => UpdatePresence(msg, callback));
        }

        internal void UpdatePresence(PresenceMessage msg, Action<bool, ErrorInfo> callback)
        {
            switch (_connection.Connection.State)
            {
                case ConnectionState.Initialized:
                case ConnectionState.Connecting:
                case ConnectionState.Disconnected:
                case ConnectionState.Connected:
                    break;
                default:
                    var error = new ErrorInfo(
                        $"Unable to enter presence channel when connection is in a ${_connection.Connection.State} state.",
                        ErrorCodes.UnableToEnterPresenceChannelInvalidState,
                        HttpStatusCode.BadRequest);
                    Logger.Warning(error.ToString());
                    ActionUtils.SafeExecute(() => callback?.Invoke(false, error), Logger, nameof(UpdatePresence));
                    return;
            }

            switch (_channel.State)
            {
                case ChannelState.Initialized:
                    if (PendingPresenceEnqueue(new QueuedPresenceMessage(msg, callback)))
                    {
                        _channel.Attach();
                    }

                    break;
                case ChannelState.Attaching:
                    PendingPresenceEnqueue(new QueuedPresenceMessage(msg, callback));

                    break;
                case ChannelState.Attached:
                    var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name)
                    {
                        Presence = new[] { msg },
                    };
                    _connection.Send(message, callback);
                    break;
                default:
                    var error = new ErrorInfo($"Unable to enter presence channel in {_channel.State} state", ErrorCodes.UnableToEnterPresenceChannelInvalidState);
                    Logger.Warning(error.ToString());
                    ActionUtils.SafeExecute(() => callback?.Invoke(false, error), Logger, nameof(UpdatePresence));
                    return;
            }
        }

        private bool PendingPresenceEnqueue(QueuedPresenceMessage msg)
        {
            if (!_connection.Options.QueueMessages)
            {
                msg.Callback?.Invoke(
                    false,
                    new ErrorInfo("Unable enqueue message because Options.QueueMessages is set to False.", _connection.Connection.ConnectionState.DefaultErrorInfo.Code, HttpStatusCode.ServiceUnavailable));

                return false;
            }

            PendingPresenceQueue.Enqueue(msg);
            return true;
        }

        internal void OnPresence(PresenceMessage[] messages, string syncChannelSerial)
        {
            try
            {
                string syncCursor = null;

                // if we got here from SYNC message
                if (syncChannelSerial != null)
                {
                    int colonPos = syncChannelSerial.IndexOf(':');
                    string serial = colonPos >= 0 ? syncChannelSerial.Substring(0, colonPos) : syncChannelSerial;

                    /* If a new sequence identifier is sent from Ably, then the client library
                     * must consider that to be the start of a new sync sequence
                     * and any previous in-flight sync should be discarded. (part of RTP18)*/
                    if (Map.IsSyncInProgress && _currentSyncChannelSerial != null
                                             && _currentSyncChannelSerial != serial)
                    {
                        EndSync();
                    }

                    StartSync();

                    syncCursor = syncChannelSerial.Substring(colonPos);
                    if (syncCursor.Length > 1)
                    {
                        _currentSyncChannelSerial = serial;
                    }
                }

                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        bool updateInternalPresence = message.ConnectionId == _channel.RealtimeClient.Connection.Id;
                        var broadcast = true;
                        switch (message.Action)
                        {
                            case PresenceAction.Enter:
                            case PresenceAction.Update:
                            case PresenceAction.Present:
                                broadcast &= Map.Put(message);
                                if (updateInternalPresence)
                                {
                                    InternalMap.Put(message);
                                }

                                break;
                            case PresenceAction.Leave:
                                broadcast &= Map.Remove(message);
                                if (updateInternalPresence && !message.IsSynthesized())
                                {
                                    InternalMap.Remove(message);
                                }

                                break;
                        }

                        if (broadcast)
                        {
                            Publish(message);
                        }
                    }
                }
                else
                {
                    Logger.Debug("Sync with no presence");
                }

                // if this is the last message in a sequence of sync updates, end the sync
                if (syncChannelSerial == null || syncCursor.Length <= 1)
                {
                    EndSync();
                    _currentSyncChannelSerial = null;
                }
            }
            catch (Exception ex)
            {
                var errInfo = new ErrorInfo(
                    $"An error occurred processing Presence Messages for channel '{_channel.Name}'. See the InnerException for more details.");
                Logger.Error($"{errInfo.Message} Error: {ex.Message}");
                _channel.OnError(errInfo);
            }
        }

        internal void StartSync()
        {
            if (!IsSyncInProgress)
            {
                Map.StartSync();
            }
        }

        private void EndSync()
        {
            if (!IsSyncInProgress)
            {
                return;
            }

            var residualMembers = Map.EndSync();

            /*
             * RTP19: ... The PresenceMessage published should contain the original attributes of the presence
             * member with the action set to LEAVE, PresenceMessage#id set to null, and the timestamp set
             * to the current time ...
             */
            foreach (var presenceMessage in residualMembers)
            {
                presenceMessage.Action = PresenceAction.Leave;
                presenceMessage.Id = null;
                presenceMessage.Timestamp = DateTimeOffset.UtcNow;
            }

            Publish(residualMembers);
            OnSyncCompleted();
        }

        // RTP17g
        private void EnterPresenceForRecordedMembersWithCurrentConnectionId()
        {
            foreach (var item in InternalMap.Values)
            {
                var clientId = item.ClientId;
                try
                {
                    var itemToSend = new PresenceMessage(PresenceAction.Enter, item.ClientId, item.Data, item.Id);
                    UpdatePresence(itemToSend, (success, info) =>
                    {
                        if (!success)
                        {
                            /*
                             * (RTP17e)  If any of the automatic ENTER presence messages published
                             * in RTP17f fail, then an UPDATE event should be emitted on the channel
                             * with resumed set to true and reason set to an ErrorInfo object with error
                             * code value 91004 and the error message string containing the message
                             * received from Ably (if applicable), the code received from Ably
                             * (if applicable) and the explicit or implicit client_id of the PresenceMessage
                             */
                            var errorString =
                                $"Cannot automatically re-enter {clientId} on channel {_channel.Name} ({info.Message})";
                            Logger.Error(errorString);
                            _channel.EmitUpdate(new ErrorInfo(errorString, 91004), true);
                        }
                    });
                }
                catch (AblyException e)
                {
                    var errorString =
                        $"Cannot automatically re-enter {clientId} on channel {_channel.Name} ({e.ErrorInfo.Message})";
                    Logger.Error(errorString);
                    _channel.EmitUpdate(new ErrorInfo(errorString, 91004), true);
                }
            }
        }

        private void Publish(params PresenceMessage[] messages)
        {
            foreach (var message in messages)
            {
                NotifySubscribers(message);
            }
        }

        private void NotifySubscribers(PresenceMessage message)
        {
            var handlers = _handlers.GetHandlers();
            if (Logger.IsDebug)
            {
                Logger.Debug("Notifying Presence handlers: " + handlers.Count());
            }

            foreach (var handler in handlers)
            {
                var loopHandler = handler;
                _channel.RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message, Logger));
            }

            var specificHandlers = _handlers.GetHandlers(message.Action.ToString());
            if (Logger.IsDebug)
            {
                Logger.Debug("Notifying specific handlers for Message: " + message.Action + ". Count: " + specificHandlers.Count());
            }

            foreach (var specificHandler in specificHandlers)
            {
                var loopHandler = specificHandler;
                _channel.RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message, Logger));
            }
        }

        internal void ChannelDetachedOrFailed(ErrorInfo error)
        {
            FailQueuedMessages(error);
            Map.Clear();
            InternalMap.Clear();
        }

        internal void ChannelSuspended(ErrorInfo error)
        {
            /*
                 * (RTP5f) If the channel enters the SUSPENDED state then all queued presence messages will fail
                 * immediately, and the PresenceMap is maintained
                 */
            FailQueuedMessages(error);
        }

        internal void ChannelAttached(ProtocolMessage attachMessage)
        {
            /* Start sync, if hasPresence is not set end sync immediately dropping all the current presence members */
            StartSync();
            var hasPresence = attachMessage != null &&
                              attachMessage.HasFlag(ProtocolMessage.Flag.HasPresence);

            if (hasPresence)
            {
                // RTP1 If [HAS_PRESENCE] flag is 1, should set presence sync as active (Doesn't necessarily mean members are available)
                if (Logger.IsDebug)
                {
                    Logger.Debug(
                        $"Protocol message has presence flag. Starting Presence SYNC. Flag: {attachMessage.Flags}");
                }

                StartSync();
                SendQueuedMessages();
            }
            else
            {
                /* RTP1 If [HAS_PRESENCE] flag is 0 or there is no flags field,
                    * the presence map should be considered in sync immediately
                    * with no members present on the channel
                    *
                    * RTP19a  If the PresenceMap has existing members when an ATTACHED message is received without a
                    * HAS_PRESENCE flag, the client library should emit a LEAVE event for each existing member ...
                    */
                EndSync();
                SendQueuedMessages();

                // TODO: Missing sending my members if any
            }
        }

        private void SendQueuedMessages()
        {
            if (PendingPresenceQueue.Count == 0)
            {
                return;
            }

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name)
            {
                Presence = new PresenceMessage[PendingPresenceQueue.Count],
            };
            var callbacks = new List<Action<bool, ErrorInfo>>();
            var i = 0;

            while (!PendingPresenceQueue.IsEmpty)
            {
                if (PendingPresenceQueue.TryDequeue(out var queuedPresenceMessage))
                {
                    message.Presence[i++] = queuedPresenceMessage.Message;
                    if (queuedPresenceMessage.Callback != null)
                    {
                        callbacks.Add(queuedPresenceMessage.Callback);
                    }
                }
            }

            _connection.Send(message, (s, e) =>
            {
                foreach (var callback in callbacks)
                {
                    callback(s, e);
                }
            });
        }

        private void FailQueuedMessages(ErrorInfo reason)
        {
            while (!PendingPresenceQueue.IsEmpty)
            {
                if (PendingPresenceQueue.TryDequeue(out var queuedPresenceMessage))
                {
                    queuedPresenceMessage.Callback?.Invoke(false, reason);
                }
            }
        }

        /// <summary>
        /// Obtain recent history for this channel using the REST API.
        /// The history provided relates to all clients of this application,
        /// not just this instance.
        /// </summary>
        /// <param name="untilAttach">optionally can add until attached parameter.</param>
        /// <exception cref="AblyException">can throw if untilAttached=true and the current channel is not attached.</exception>
        /// <returns>Paginated list of Presence messages.</returns>
        public Task<PaginatedResult<PresenceMessage>> HistoryAsync(bool untilAttach = false)
        {
            var query = new PaginatedRequestParams();
            if (untilAttach)
            {
                _channel.AddUntilAttachParameter(query);
            }

            return _channel.RestChannel.Presence.HistoryAsync(query);
        }

        /// <summary>
        /// Obtain recent history for this channel using the REST API.
        /// The history provided relates to all clients of this application,
        /// not just this instance.
        /// </summary>
        /// <param name="query">the request params. See the Ably REST API documentation for more details.</param>
        /// <param name="untilAttach">add until attached parameter.</param>
        /// <exception cref="AblyException">can throw if untilAttached=true and the current channel is not attached.</exception>
        /// <returns>Paginated list of Presence messages.</returns>
        public Task<PaginatedResult<PresenceMessage>> HistoryAsync(PaginatedRequestParams query, bool untilAttach = false)
        {
            query = query ?? new PaginatedRequestParams();
            if (untilAttach)
            {
                _channel.AddUntilAttachParameter(query);
            }

            return _channel.RestChannel.Presence.HistoryAsync(query);
        }

        private void OnSyncCompleted()
        {
            SyncCompleted?.Invoke(this, EventArgs.Empty);
        }

        internal JToken GetState() => new JObject
        {
            ["handlers"] = _handlers.GetState(),
            ["members"] = Map.GetState(),
            ["pendingQueue"] = new JArray(PendingPresenceQueue.Select(x => JObject.FromObject(x.Message))),
        };

        private void OnInitialSyncCompleted()
        {
            InitialSyncCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios. If disposing equals true, the method has
        /// been called directly or indirectly by a user's code. Managed and unmanaged resources can be disposed.
        /// If disposing equals false, the method has been called by the runtime from inside the finalizer and
        /// you should not reference other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing">Refer to the summary.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _handlers.Dispose();
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
        }
    }
}
