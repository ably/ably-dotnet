﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public partial class Presence : IDisposable
    {
        internal ILogger Logger { get; private set; }

        private event EventHandler InitialSyncCompleted;

        private readonly RealtimeChannel _channel;
        private readonly string _clientId;
        private readonly Handlers<PresenceMessage> _handlers = new Handlers<PresenceMessage>();

        private readonly IConnectionManager _connection;
        private readonly List<QueuedPresenceMessage> _pendingPresence;

        private string _currentSyncChannelSerial;
        private bool _syncAsResultOfAttach;

        private bool _initialSyncCompleted = false;

        public bool SyncComplete
        {
            get { return Map.InitialSyncCompleted | _initialSyncCompleted; }

            private set
            {
                _initialSyncCompleted = value;
                if (_initialSyncCompleted)
                {
                    OnInitialSyncCompleted();
                }
            }
        }

        public bool IsSyncInProgress => Map.IsSyncInProgress;

        /// <summary>
        /// Called when a protocol message HasPresenceFlag == false. The presence map should be considered in sync immediately
        /// with no members present on the channel. See [RTP1] for more detail.
        /// </summary>
        internal void SkipSync()
        {
            SyncComplete = true;
        }

        internal Presence(IConnectionManager connection, RealtimeChannel channel, string cliendId, ILogger logger)
        {
            Logger = logger;
            Map = new PresenceMap(channel.Name, logger);
            InternalMap = new PresenceMap(channel.Name, logger);
            _pendingPresence = new List<QueuedPresenceMessage>();
            _connection = connection;
            _connection.Connection.ConnectionStateChanged += OnConnectionStateChanged;
            _channel = channel;
            _channel.InternalStateChanged += OnChannelStateChanged;
            _clientId = cliendId;
        }

        internal bool InternalSyncComplete => !Map.IsSyncInProgress && SyncComplete;

        internal PresenceMap Map { get; }

        internal PresenceMap InternalMap { get; }

        public void Dispose()
        {
            if (_channel != null)
            {
                _channel.InternalStateChanged -= OnChannelStateChanged;
            }

            if (_connection != null)
            {
                _connection.Connection.ConnectionStateChanged -= OnConnectionStateChanged;
            }

            _handlers.RemoveAll();
        }

        /// <summary>
        ///     Get current presence in the channel. WaitForSync is not implemented yet. Partial result may be returned
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<PresenceMessage>> GetAsync(GetParams options)
        {
            // RTP11b
            if (_channel.State == ChannelState.Failed || _channel.State == ChannelState.Detached)
            {
                throw new AblyException(new ErrorInfo($"channel operation failed. Invalid channel state ({_channel.State})", 90001));
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
                await WaitForSyncAsync();
            }

            var result = Map.Values.Where(x => (getOptions.ClientId.IsEmpty() || x.ClientId == getOptions.ClientId)
                                               && (getOptions.ConnectionId.IsEmpty() || x.ConnectionId == getOptions.ConnectionId));
            return result;
        }

        public async Task<IEnumerable<PresenceMessage>> GetAsync(bool waitForSync)
        {
            return await GetAsync(new GetParams() { WaitForSync = waitForSync });
        }

        public async Task<IEnumerable<PresenceMessage>> GetAsync(string clientId, bool waitForSync)
        {
            return await GetAsync(new GetParams() { ClientId = clientId, WaitForSync = waitForSync });
        }

        public async Task<IEnumerable<PresenceMessage>> GetAsync(string clientId = null, string connectionId = null, bool waitForSync = true)
        {
            return await GetAsync(new GetParams() { ClientId = clientId, ConnectionId = connectionId, WaitForSync = waitForSync });
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
            void OnChannelOnStateChanged(object sender, ChannelStateChange args)
            {
                if (_channel.State != ChannelState.Attached && _channel.State != ChannelState.Attaching)
                {
                    tsc.TrySetResult(false);
                }
            }

            void OnSyncEvent(object sender, EventArgs args) => CheckAndSet();

            _channel.StateChanged += OnChannelOnStateChanged;
            InitialSyncCompleted += OnSyncEvent;
            Map.SyncNoLongerInProgress += OnSyncEvent;

            // Do a manual check in case we are already in the desired state
            CheckAndSet();
            bool syncIsComplete = await tsc.Task;

            // unsubscribe from events
            _channel.StateChanged -= OnChannelOnStateChanged;
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
                    errorCode = 90001;
                    errorMessage = $"Channel {_channel.Name}: cannot get presence state when the channel is in a {_channel.State} state.";
                }

                Logger.Debug($"{errorMessage} (Error Code: {errorCode})");
                throw new AblyException(new ErrorInfo(errorMessage, errorCode));
            }

            return tsc.Task.Result;
        }

        public void Subscribe(Action<PresenceMessage> handler)
        {
            if (_channel.State != ChannelState.Attached && _channel.State != ChannelState.Attaching)
            {
                _channel.Attach();
            }

            _handlers.Add(handler.ToHandlerAction());
        }

        public void Subscribe(PresenceAction action, Action<PresenceMessage> handler)
        {
            if ((_channel.State != ChannelState.Attached) && (_channel.State != ChannelState.Attaching))
            {
                _channel.Attach();
            }

            _handlers.Add(action.ToString(), new MessageHandlerAction<PresenceMessage>(handler));
        }

        public bool Unsubscribe(Action<PresenceMessage> handler)
        {
            return _handlers.Remove(handler.ToHandlerAction());
        }

        public void Unsubscribe()
        {
            _handlers.RemoveAll();
        }

        public bool Unsubscribe(PresenceAction presenceAction, Action<PresenceMessage> handler)
        {
            return _handlers.Remove(presenceAction.ToString(), handler.ToHandlerAction());
        }

        public void Enter(object data = null, Action<bool, ErrorInfo> callback = null)
        {
            EnterClient(_clientId, data, callback);
        }

        public Task<Result> EnterAsync(object data = null)
        {
            return EnterClientAsync(_clientId, data);
        }

        public void EnterClient(string clientId, object data, Action<bool, ErrorInfo> callback = null)
        {
            UpdatePresence(new PresenceMessage(PresenceAction.Enter, clientId, data), callback);
        }

        public Task<Result> EnterClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Enter, clientId, data));
        }

        public void Update(object data = null, Action<bool, ErrorInfo> callback = null)
        {
            UpdateClient(_clientId, data, callback);
        }

        public Task<Result> UpdateAsync(object data = null)
        {
            return UpdateClientAsync(_clientId, data);
        }

        public void UpdateClient(string clientId, object data, Action<bool, ErrorInfo> callback = null)
        {
            UpdatePresence(new PresenceMessage(PresenceAction.Update, clientId, data), callback);
        }

        public Task<Result> UpdateClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Update, clientId, data));
        }

        public void Leave(object data = null, Action<bool, ErrorInfo> callback = null)
        {
            LeaveClient(_clientId, data);
        }

        public Task<Result> LeaveAsync(object data = null)
        {
            return LeaveClientAsync(_clientId, data);
        }

        public void LeaveClient(string clientId, object data, Action<bool, ErrorInfo> callback = null)
        {
            UpdatePresence(new PresenceMessage(PresenceAction.Leave, clientId, data), callback);
        }

        public Task<Result> LeaveClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Leave, clientId, data));
        }

        internal Task<Result> UpdatePresenceAsync(PresenceMessage msg)
        {
            var tw = new TaskWrapper();
            UpdatePresence(msg, tw.Callback);
            return tw.Task;
        }

        internal void UpdatePresence(PresenceMessage msg, Action<bool, ErrorInfo> callback)
        {
            switch (_channel.State)
            {
                case ChannelState.Initialized:
                    _channel.Attach();
                    _pendingPresence.Add(new QueuedPresenceMessage(msg, callback));
                    break;
                case ChannelState.Attaching:
                    _pendingPresence.Add(new QueuedPresenceMessage(msg, callback));
                    break;
                case ChannelState.Attached:
                    var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name);
                    message.Presence = new[] { msg };
                    _connection.Send(message, callback);
                    break;
                default:
                    throw new AblyException("Unable to enter presence channel in detached or failed state", 91001, HttpStatusCode.BadRequest);
            }
        }

        internal void ResumeSync()
        {
            if (_channel.State == ChannelState.Initialized ||
                _channel.State == ChannelState.Detached ||
                _channel.State == ChannelState.Detaching)
            {

                throw new AblyException("Unable to sync to channel; not attached", 40000, HttpStatusCode.BadRequest);
            }

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Sync, _channel.Name);
            message.ChannelSerial = _currentSyncChannelSerial;
            _connection.Send(message, null);
        }

        internal void OnPresence(PresenceMessage[] messages, string syncChannelSerial)
        {
            try
            {
                string syncCursor = null;
                if (syncChannelSerial != null)
                {
                    syncCursor = syncChannelSerial.Substring(syncChannelSerial.IndexOf(':'));
                    if (syncCursor.Length > 1)
                    {
                        Map.StartSync();
                    }
                }

                if (syncChannelSerial != null)
                {
                    int colonPos = syncChannelSerial.IndexOf(':');
                    string serial = colonPos >= 0 ? syncChannelSerial.Substring(0, colonPos) : syncChannelSerial;

                    /* Discard incomplete sync if serial has changed */
                    if (Map.IsSyncInProgress && _currentSyncChannelSerial != null
                                             && _currentSyncChannelSerial != serial)
                    {
                        /* TODO: For v1.0 we should emit leave messages here. See https://github.com/ably/ably-java/blob/159018c30b3ef813a9d3ca3c6bc82f51aacbbc68/lib/src/main/java/io/ably/lib/realtime/Presence.java#L219 for example. */
                        _currentSyncChannelSerial = null;
                        Task.Run(EndSync);
                    }

                    syncCursor = syncChannelSerial.Substring(colonPos);
                    if (syncCursor.Length > 1)
                    {
                        StartSync();
                        _currentSyncChannelSerial = serial;
                    }
                }

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
                            if (updateInternalPresence)
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

                // if this is the last message in a sequence of sync updates, end the sync
                if ((syncChannelSerial == null) || (syncCursor.Length <= 1))
                {
                    Task.Run(EndSync);
                }
            }
            catch (Exception ex)
            {
                var errInfo = new ErrorInfo(
                    $"An error occurred processing Presence Messages for channel '{_channel.Name}'. See the InnerException for more details.");
                _channel.SetChannelState(ChannelState.Failed, errInfo);
                Logger.Error($"{errInfo.Message} Error: {ex.Message}");
                errInfo.Message += " See the InnerException for more details.";
                throw new AblyException(errInfo, ex);
            }
        }

        internal void StartSync()
        {
            if (!IsSyncInProgress)
            {
                Map.StartSync();
            }
        }

        internal async Task EndSync()
        {
            _currentSyncChannelSerial = null;
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

            /*
             * (RTP5c2) If a SYNC is initiated as part of the attach, then once the SYNC is complete,
             * all members not present in the PresenceMap but present in the internal PresenceMap must
             * be re-entered automatically by the client using the clientId and data attributes from
             * each. The members re-entered automatically must be removed from the internal PresenceMap
             * ensuring that members present on the channel are constructed from presence events sent
             * from Ably since the channel became ATTACHED
             */
            if (_syncAsResultOfAttach)
            {
                _syncAsResultOfAttach = false;
                foreach (var item in InternalMap.Values)
                {
                    if (Map.Put(item))
                    {
                        var clientId = item.ClientId;
                        try
                        {
                            /* Message is new to presence map, send it */
                            var itemToSend = item.ShallowClone();
                            itemToSend.Action = PresenceAction.Enter;
                            var result = await UpdatePresenceAsync(itemToSend);
                            if (result.IsFailure)
                            {
                                /*
                                 * (RTP5c3)  If any of the automatic ENTER presence messages published
                                 * in RTP5c2 fail, then an UPDATE event should be emitted on the channel
                                 * with resumed set to true and reason set to an ErrorInfo object with error
                                 * code value 91004 and the error message string containing the message
                                 * received from Ably (if applicable), the code received from Ably
                                 * (if applicable) and the explicit or implicit client_id of the PresenceMessage
                                 */
                                var errorString = $"Cannot automatically re-enter {clientId} on channel {_channel.Name} ({result.Error.Message})";
                                Logger.Error(errorString);
                                _channel.EmitUpdate(new ErrorInfo(errorString, 91004), true);
                            }
                        }
                        catch (AblyException e)
                        {
                            var errorString = $"Cannot automatically re-enter {clientId} on channel {_channel.Name} ({e.ErrorInfo.Message})";
                            Logger.Error(errorString);
                            _channel.EmitUpdate(new ErrorInfo(errorString, 91004), true);
                        }

                    }
                }

                InternalMap.Clear();
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

        private void OnChannelStateChanged(object sender, ChannelStateChange e)
        {
            if (e.Current == ChannelState.Attached)
            {
                /* Start sync, if hasPresence is not set end sync immediately dropping all the current presence members */
                StartSync();
                _syncAsResultOfAttach = true;
                var hasPresence = e.ProtocolMessage != null && e.ProtocolMessage.HasFlag(ProtocolMessage.Flag.HasPresence);
                if (!hasPresence)
                {
                    /*
                    * RTP19a  If the PresenceMap has existing members when an ATTACHED message is received without a
                    * HAS_PRESENCE flag, the client library should emit a LEAVE event for each existing member ...
                    */
                    Task.Run(async () =>
                    {
                        await EndSync();
                        SendQueuedMessages();
                    });
                }
                else
                {
                    SendQueuedMessages();
                }

                SendQueuedMessages();
            }
            else if (e.Current == ChannelState.Detached || e.Current == ChannelState.Failed)
            {
                FailQueuedMessages(e.Error);
                Map.Clear();
                InternalMap.Clear();
            }
            else if (e.Current == ChannelState.Suspended)
            {
                /*
		         * (RTP5f) If the channel enters the SUSPENDED state then all queued presence messages will fail
		         * immediately, and the PresenceMap is maintained
		         */
                FailQueuedMessages(e.Error);
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChange e)
        {
            if (!Map.IsSyncInProgress && _connection.Connection.State == ConnectionState.Connected)
            {
                ResumeSync();
            }
        }

        private void SendQueuedMessages()
        {
            if (_pendingPresence.Count == 0)
            {
                return;
            }

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name);
            message.Presence = new PresenceMessage[_pendingPresence.Count];
            var callbacks = new List<Action<bool, ErrorInfo>>();
            var i = 0;
            foreach (var presenceMessage in _pendingPresence)
            {
                message.Presence[i++] = presenceMessage.Message;
                if (presenceMessage.Callback != null)
                {
                    callbacks.Add(presenceMessage.Callback);
                }
            }

            _pendingPresence.Clear();

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
            foreach (var presenceMessage in _pendingPresence.Where(c => c.Callback != null))
            {
                presenceMessage.Callback(false, reason);
            }

            _pendingPresence.Clear();
        }

        public Task<PaginatedResult<PresenceMessage>> HistoryAsync(bool untilAttach = false)
        {
            var query = new PaginatedRequestParams();
            if (untilAttach)
            {
                _channel.AddUntilAttachParameter(query);
            }

            return _channel.RestChannel.Presence.HistoryAsync(query);
        }

        public Task<PaginatedResult<PresenceMessage>> HistoryAsync(PaginatedRequestParams query, bool untilAttach = false)
        {
            query = query ?? new PaginatedRequestParams();
            if (untilAttach)
            {
                _channel.AddUntilAttachParameter(query);
            }

            return _channel.RestChannel.Presence.HistoryAsync(query);
        }

        protected virtual void OnInitialSyncCompleted()
        {
            InitialSyncCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
