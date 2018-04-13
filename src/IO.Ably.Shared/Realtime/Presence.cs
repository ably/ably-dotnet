using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public class GetOptions
    {
        public bool WaitForSync { get; set; } = true;
        public string ClientId { get; set; }
        public string ConnectionId { get; set; }
    }

    public class Presence : IDisposable
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
                if(_initialSyncCompleted)
                    OnInitialSyncCompleted();
            }
        }
        
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
            _channel = channel;
            _channel.InternalStateChanged += OnChannelStateChanged;
            _clientId = cliendId;
        }
        
        internal bool InternalSyncComplete => (!Map.IsSyncInProgress && SyncComplete);
        internal PresenceMap Map { get; }
        internal PresenceMap InternalMap { get; }

        public void Dispose()
        {
            if(_channel != null)
                _channel.InternalStateChanged -= OnChannelStateChanged;

            _handlers.RemoveAll();
        }

        /// <summary>
        ///     Get current presence in the channel. WaitForSync is not implemented yet. Partial result may be returned
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<PresenceMessage>> GetAsync(GetOptions options = null)
        {
            var getOptions = options ?? new GetOptions();
            
            if (getOptions.WaitForSync)
                await WaitForSyncAsync();

            var result = Map.Values.Where( x => (getOptions.ClientId.IsEmpty() || x.ClientId == getOptions.ClientId)
                                               && (getOptions.ConnectionId.IsEmpty() || x.ConnectionId == getOptions.ConnectionId) );
            return result;
        }

        internal async Task<IEnumerable<PresenceMessage>> GetAsync(string clientId, bool waitForSync = false)
        {
            return await GetAsync(new GetOptions() {ClientId = clientId, WaitForSync = waitForSync});
        }

        internal async Task<IEnumerable<PresenceMessage>> GetAsync(string clientId, string connectionId, bool waitForSync = true)
        {
            return await GetAsync(new GetOptions() { ClientId = clientId, ConnectionId = connectionId, WaitForSync = waitForSync });
        }

        private async Task<bool> WaitForSyncAsync()
        {
            var tsc = new TaskCompletionSource<bool>();
            // The InternalSync should be completed and the channels Attached or Attaching
            void CheckAndSet()
            {
                if (InternalSyncComplete &&
                    (_channel.State == ChannelState.Attached || _channel.State == ChannelState.Attaching))
                {
                    tsc.TrySetResult(true);
                }
            }
            // if the channel state changes and is not Attached or Attaching then we should exit
            void OnChannelOnStateChanged(object sender, ChannelStateChange args)
            {
                if (_channel.State != ChannelState.Attached && _channel.State != ChannelState.Attaching)
                    tsc.TrySetResult(false);
            }
            void OnSyncEvent(object sender, EventArgs args) => CheckAndSet();

            _channel.StateChanged += OnChannelOnStateChanged;
            InitialSyncCompleted += OnSyncEvent;
            Map.SyncNoLongerInProgress += OnSyncEvent;
            Map.InitialSyncHasCompleted += OnSyncEvent;
            // Do a manual check in case we are already in the desired state
            CheckAndSet();
            await tsc.Task;
            // unsubscribe from events 
            _channel.StateChanged -= OnChannelOnStateChanged;
            InitialSyncCompleted -= OnSyncEvent;
            Map.SyncNoLongerInProgress -= OnSyncEvent;
            Map.InitialSyncHasCompleted -= OnSyncEvent;

            #region TODO
            // TODO: This code was added when porting from the Java lib but can't completed be until the extra states added in 1.0 spec (specifically ChannelState.Suspended) are implemented
            //bool syncIsComplete =  tsc.Task.Result;
            //      if (!syncIsComplete)
            //      {
            //          /* invalid channel state */
            //          int errorCode;
            //          String errorMessage;

            //          if (_channel.State == ChannelState.Suspended)
            //          {
            //              /* (RTP11d) If the Channel is in the SUSPENDED state then the get function will by default,
            //* or if waitForSync is set to true, result in an error with code 91005 and a message stating
            //* that the presence state is out of sync due to the channel being in a SUSPENDED state */
            //              errorCode = 91005;
            //              errorMessage = $"Channel {_channel.Name}: presence state is out of sync due to the channel being in a SUSPENDED state";
            //          }
            //          else
            //          {
            //              errorCode = 90001;
            //              errorMessage = $"Channel {_channel.Name}: cannot get presence state because channel is in invalid state";
            //          }
            //          if(Logger.IsDebug)
            //              Logger.Debug($"{errorMessage} (Error Code: {errorCode})");
            //          throw new AblyException(new ErrorInfo(errorMessage, errorCode));
            //      }


            #endregion
            
            return tsc.Task.Result;
        }
        

        public void Subscribe(Action<PresenceMessage> handler)
        {
            if ((_channel.State != ChannelState.Attached) && (_channel.State != ChannelState.Attaching))
                _channel.Attach();

            _handlers.Add(handler.ToHandlerAction());
        }

        public void Subscribe(PresenceAction action, Action<PresenceMessage> handler)
        {
            if ((_channel.State != ChannelState.Attached) && (_channel.State != ChannelState.Attaching))
                _channel.Attach();

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

        public Task EnterAsync(object data = null)
        {
            return EnterClientAsync(_clientId, data);
        }

        public Task EnterClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Enter, clientId, data));
        }

        public Task UpdateAsync(object data = null)
        {
            return UpdateClientAsync(_clientId, data);
        }

        public Task UpdateClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Update, clientId, data));
        }

        public Task LeaveAsync(object data = null)
        {
            return LeaveClientAsync(_clientId, data);
        }

        public Task LeaveClientAsync(string clientId, object data)
        {
            return UpdatePresenceAsync(new PresenceMessage(PresenceAction.Leave, clientId, data));
        }

        internal Task UpdatePresenceAsync(PresenceMessage msg)
        {
            if ((_channel.State == ChannelState.Initialized) || (_channel.State == ChannelState.Attaching))
            {
                if (_channel.State == ChannelState.Initialized)
                    _channel.Attach();

                var tw = new TaskWrapper();
                _pendingPresence.Add(new QueuedPresenceMessage(msg, tw.Callback));
                return tw.Task;
            }
            if (_channel.State == ChannelState.Attached)
            {
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name);
                message.Presence = new[] {msg};
                _connection.Send(message, null);
                //TODO: Fix this;
                return TaskConstants.BooleanTrue;
            }
            throw new AblyException("Unable to enter presence channel in detached or failed state", 91001,
                HttpStatusCode.BadRequest);
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
                        Map.StartSync();
                }

                if (syncChannelSerial != null)
                {
                    int colonPos = syncChannelSerial.IndexOf(':');
                    string serial = colonPos >= 0 ? syncChannelSerial.Substring(0, colonPos) : syncChannelSerial;
                    /* Discard incomplete sync if serial has changed */
                    if (Map.IsSyncInProgress && _currentSyncChannelSerial != null &&
                        _currentSyncChannelSerial != serial)
                    {
                        /* TODO: For v1.0 we should emit leave messages here. See https://github.com/ably/ably-java/blob/159018c30b3ef813a9d3ca3c6bc82f51aacbbc68/lib/src/main/java/io/ably/lib/realtime/Presence.java#L219 for example. */
                        EndSync();
                    }

                    syncCursor = syncChannelSerial.Substring(colonPos);
                    if (syncCursor.Length > 1)
                    {
                        Map.StartSync();
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
                                InternalMap.Put(message);
                            break;
                        case PresenceAction.Leave:
                            broadcast &= Map.Remove(message);
                            if (updateInternalPresence)
                                InternalMap.Remove(message);
                            break;
                    }
                    if (broadcast)
                        Publish(message);
                }
                // if this is the last message in a sequence of sync updates, end the sync
                if ((syncChannelSerial == null) || (syncCursor.Length <= 1))
                    EndSync();

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

        internal void EndSync()
        {
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
                        /* Message is new to presence map, send it */
                        string clientId = item.ClientId;
                        var itemToSend = item.ShallowClone();
                        itemToSend.Action = PresenceAction.Enter;
                        UpdatePresenceAsync(itemToSend);

                    }
                }
                InternalMap.Clear();
            }
        }

        private void Publish(params PresenceMessage[] messages)
        {
            foreach (var message in messages)
                NotifySubscribers(message);
        }

        private void NotifySubscribers(PresenceMessage message)
        {
            var handlers = _handlers.GetHandlers();
            if(Logger.IsDebug) Logger.Debug("Notifying Presence handlers: " + handlers.Count());
            foreach (var handler in handlers)
            {
                var loopHandler = handler;
                _channel.RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message, Logger));
            }

            var specificHandlers = _handlers.GetHandlers(message.Action.ToString());
            if(Logger.IsDebug) Logger.Debug("Notifying specific handlers for Message: " + message.Action + ". Count: " + specificHandlers.Count());
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
                Map.StartSync();
                _syncAsResultOfAttach = true;
                // TODO: for v1.0 RTP19a (see Java version for example https://github.com/ably/ably-java/blob/159018c30b3ef813a9d3ca3c6bc82f51aacbbc68/lib/src/main/java/io/ably/lib/realtime/Presence.java)
                //if (!hasPresence)
                //{
                //    /*
                //     * RTP19a  If the PresenceMap has existing members when an ATTACHED message is received without a
                //     * HAS_PRESENCE flag, the client library should emit a LEAVE event for each existing member ...
                //     */
                //    endSyncAndEmitLeaves();
                //}
                SendQueuedMessages();
            }
            else if ((e.Current == ChannelState.Detached) || (e.Current == ChannelState.Failed))
            {
                FailQueuedMessages(e.Error);
                Map.Clear();
                InternalMap.Clear();
            }
        }

        private void SendQueuedMessages()
        {
            if (_pendingPresence.Count == 0)
                return;

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name);
            message.Presence = new PresenceMessage[_pendingPresence.Count];
            var callbacks = new List<Action<bool, ErrorInfo>>();
            var i = 0;
            foreach (var presenceMessage in _pendingPresence)
            {
                message.Presence[i++] = presenceMessage.Message;
                if (presenceMessage.Callback != null)
                    callbacks.Add(presenceMessage.Callback);
            }
            _pendingPresence.Clear();

            _connection.Send(message, (s, e) =>
            {
                foreach (var callback in callbacks)
                    callback(s, e);
            });
        }

        private void FailQueuedMessages(ErrorInfo reason)
        {
            foreach (var presenceMessage in _pendingPresence.Where(c => c.Callback != null))
                presenceMessage.Callback(false, reason);
            _pendingPresence.Clear();
        }

        public Task<PaginatedResult<PresenceMessage>> HistoryAsync(bool untilAttach = false)
        {
            var query = new HistoryRequestParams();
            if (untilAttach)
                _channel.AddUntilAttachParameter(query);
            return _channel.RestChannel.Presence.HistoryAsync(query);
        }

        public Task<PaginatedResult<PresenceMessage>> HistoryAsync(HistoryRequestParams query, bool untilAttach = false)
        {
            query = query ?? new HistoryRequestParams();
            if (untilAttach)
                _channel.AddUntilAttachParameter(query);

            return _channel.RestChannel.Presence.HistoryAsync(query);
        }

        public void AwaitSync()
        {
            Map.StartSync();
        }

        internal class PresenceMap
        {
            internal ILogger Logger { get; private set; }

            internal event EventHandler SyncNoLongerInProgress;
            internal event EventHandler InitialSyncHasCompleted;

            private readonly string _channelName;
            private readonly object _lock = new Object();

            public enum State
            {
                Initialized,
                SyncStarting,
                InSync,
                Failed
            }

            private readonly ConcurrentDictionary<string, PresenceMessage> _members;
            private ICollection<string> _residualMembers;
            private bool _isSyncInProgress;
            private bool _initialSyncCompleted;

            public PresenceMap(string channelName, ILogger logger)
            {
                Logger = logger;
                _channelName = channelName;
                _members = new ConcurrentDictionary<string, PresenceMessage>();
            }

            public bool IsSyncInProgress
            {
                get => _isSyncInProgress;
                private set
                {
                    var previous = _isSyncInProgress;
                    _isSyncInProgress = value;
                    // if we have gone from true to false then fire SyncNoLongerInProgress
                    if (previous && !_isSyncInProgress)
                        OnSyncNoLongerInProgress();
                }
            }

            public bool InitialSyncCompleted
            {
                get => _initialSyncCompleted;
                private set
                {
                    var previous = _initialSyncCompleted;
                    _initialSyncCompleted = value;
                    if (!previous && _initialSyncCompleted)
                        OnInitialSyncHasCompleted();
                }
            }

            public PresenceMessage[] Values
            {
                get
                {
                    return _members.Values.Where(c => c.Action != PresenceAction.Absent)
                        .ToArray();
                }
            }

            public bool Put(PresenceMessage item)
            {
                lock (_lock)
                {
                    // we've seen this member, so do not remove it at the end of sync
                    _residualMembers?.Remove(item.MemberKey);
                }

                try
                {
                    if (_members.TryGetValue(item.MemberKey, out var existingItem) && existingItem.IsNewerThan(item))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"PresenceMap.Put | Channel: {_channelName}, Error: {ex.Message}");
                    throw ex;
                }

                switch (item.Action)
                {
                    case PresenceAction.Enter:
                    case PresenceAction.Update:
                        item = item.ShallowClone();
                        item.Action = PresenceAction.Present;
                        break;
                }

                _members[item.MemberKey] = item;

                return true;
            }

            public bool Remove(PresenceMessage item)
            {
                PresenceMessage existingItem;
                if (_members.TryGetValue(item.MemberKey, out existingItem) && existingItem.IsNewerThan(item))
                    return false;

                _members.TryRemove(item.MemberKey, out PresenceMessage _);
                if (existingItem?.Action == PresenceAction.Absent)
                    return false;

                return true;
            }

            public void StartSync()
            {
                if (Logger.IsDebug)
                    Logger.Debug($"StartSync | Channel: {_channelName}, SyncInProgress: {IsSyncInProgress}");
                if (!IsSyncInProgress)
                {
                    lock (_lock)
                    {
                        _residualMembers = new HashSet<string>(_members.Keys);
                        IsSyncInProgress = true;
                    }
                }
            }

            public PresenceMessage[] EndSync()
            {
                if (Logger.IsDebug)
                    Logger.Debug($"EndSync | Channel: {_channelName}, SyncInProgress: {IsSyncInProgress}");
                List<PresenceMessage> removed = new List<PresenceMessage>();
                try
                {
                    if (!IsSyncInProgress) return removed.ToArray();
                    // We can now strip out the ABSENT members, as we have
                    // received all of the out-of-order sync messages
                    foreach (var member in _members.ToArray())
                    {
                        if (member.Value.Action == PresenceAction.Absent)
                        {
                            _members.TryRemove(member.Key, out PresenceMessage _);
                        }
                    }

                    lock (_lock)
                    {
                        if (_residualMembers != null)
                        {
                            // Any members that were present at the start of the sync,
                            // and have not been seen in sync, can be removed
                            foreach (var member in _residualMembers)
                            {
                                if (_members.TryRemove(member, out PresenceMessage pm))
                                    removed.Add(pm);
                            }
                            _residualMembers = null;
                        }
                        IsSyncInProgress = false;
                    }
                    
                }
                catch (Exception ex)
                {
                    Logger.Error($"PresenceMap.EndSync | Channel: {_channelName}, Error: {ex.Message}");
                    throw ex;
                }
                finally
                {
                    InitialSyncCompleted = true;
                }
                return removed.ToArray();
            }

            public void Clear()
            {
                lock (_lock)
                {
                    _members?.Clear();
                    _residualMembers?.Clear();
                }
            }

            protected virtual void OnSyncNoLongerInProgress()
            {
                SyncNoLongerInProgress?.Invoke(this, EventArgs.Empty);
            }

            protected virtual void OnInitialSyncHasCompleted()
            {
                InitialSyncHasCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void OnInitialSyncCompleted()
        {
            InitialSyncCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    internal class QueuedPresenceMessage
    {
        public QueuedPresenceMessage(PresenceMessage message, Action<bool, ErrorInfo> callback)
        {
            Message = message;
            Callback = callback;
        }

        public PresenceMessage Message { get; }
        public Action<bool, ErrorInfo> Callback { get; }
    }
}