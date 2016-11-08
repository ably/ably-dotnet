using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        private readonly RealtimeChannel _channel;
        private readonly string _clientId;
        private readonly Handlers<PresenceMessage> _handlers = new Handlers<PresenceMessage>();

        private readonly IConnectionManager connection;
        private readonly List<QueuedPresenceMessage> pendingPresence;

        internal Presence(IConnectionManager connection, RealtimeChannel channel, string cliendId)
        {
            Map = new PresenceMap(channel.Name);
            pendingPresence = new List<QueuedPresenceMessage>();
            this.connection = connection;
            _channel = channel;
            _channel.InternalStateChanged += OnChannelStateChanged;
            _clientId = cliendId;
        }

        //TODO: Validate the logic is correct

        public bool SyncComplete => Map.IsSyncInProgress == false;
        internal PresenceMap Map { get; }

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
        public Task<IEnumerable<PresenceMessage>> GetAsync(GetOptions options = null)
        {
            var getOptions = options ?? new GetOptions();

            //TODO: waitForSync is not implemented yet
            var result = Map.Values.Where(x => (getOptions.ClientId.IsEmpty() || (x.ClientId == getOptions.ClientId))
                                               &&
                                               (getOptions.ConnectionId.IsEmpty() ||
                                                (x.ConnectionId == getOptions.ConnectionId)));
            return Task.FromResult(result);
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
                pendingPresence.Add(new QueuedPresenceMessage(msg, tw.Callback));
                return tw.Task;
            }
            if (_channel.State == ChannelState.Attached)
            {
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name);
                message.Presence = new[] {msg};
                connection.Send(message, null);
                //TODO: Fix this;
                return TaskConstants.BooleanTrue;
            }
            throw new AblyException("Unable to enter presence channel in detached or failed state", 91001,
                HttpStatusCode.BadRequest);
        }

        internal void OnPresence(PresenceMessage[] messages, string syncChannelSerial)
        {
            string syncCursor = null;
            var broadcast = true;
            if (syncChannelSerial != null)
            {
                syncCursor = syncChannelSerial.Substring(syncChannelSerial.IndexOf(':'));
                if (syncCursor.Length > 1)
                    Map.StartSync();
            }

            foreach (var update in messages)
                switch (update.Action)
                {
                    case PresenceAction.Enter:
                    case PresenceAction.Update:
                    case PresenceAction.Present:
                        broadcast &= Map.Put(update);
                        break;
                    case PresenceAction.Leave:
                        broadcast &= Map.Remove(update);
                        break;
                }
            // if this is the last message in a sequence of sync updates, end the sync
            if ((syncChannelSerial == null) || (syncCursor.Length <= 1))
                Map.EndSync();

            if (broadcast)
                Publish(messages);
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
                _channel.RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message));
            }

            var specificHandlers = _handlers.GetHandlers(message.Action.ToString());
            if(Logger.IsDebug) Logger.Debug("Notifying specific handlers for Message: " + message.Action + ". Count: " + specificHandlers.Count());
            foreach (var specificHandler in specificHandlers)
            {
                var loopHandler = specificHandler;
                _channel.RealtimeClient.NotifyExternalClients(() => loopHandler.SafeHandle(message));
            }
        }

        private void OnChannelStateChanged(object sender, ChannelStateChange e)
        {
            if (e.Current == ChannelState.Attached)
            {
                SendQueuedMessages();
            }
            else if ((e.Current == ChannelState.Detached) || (e.Current == ChannelState.Failed))
                FailQueuedMessages(e.Error);
        }

        private void SendQueuedMessages()
        {
            if (pendingPresence.Count == 0)
                return;

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channel.Name);
            message.Presence = new PresenceMessage[pendingPresence.Count];
            var callbacks = new List<Action<bool, ErrorInfo>>();
            var i = 0;
            foreach (var presenceMessage in pendingPresence)
            {
                message.Presence[i++] = presenceMessage.Message;
                if (presenceMessage.Callback != null)
                    callbacks.Add(presenceMessage.Callback);
            }
            pendingPresence.Clear();

            connection.Send(message, (s, e) =>
            {
                foreach (var callback in callbacks)
                    callback(s, e);
            });
        }

        private void FailQueuedMessages(ErrorInfo reason)
        {
            foreach (var presenceMessage in pendingPresence.Where(c => c.Callback != null))
                presenceMessage.Callback(false, reason);
            pendingPresence.Clear();
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
            private readonly string _channelName;

            public enum State
            {
                Initialized,
                SyncStarting,
                InSync,
                Failed
            }

            private readonly Dictionary<string, PresenceMessage> members;
            private ICollection<string> residualMembers;

            public PresenceMap(string channelName)
            {
                _channelName = channelName;
                members = new Dictionary<string, PresenceMessage>();
            }

            public bool IsSyncInProgress { get; private set; }

            public PresenceMessage[] Values
            {
                get
                {
                    return members.Values.Where(c => c.Action != PresenceAction.Absent)
                        .ToArray();
                }
            }

            public bool Put(PresenceMessage item)
            {
                // we've seen this member, so do not remove it at the end of sync
                residualMembers?.Remove(item.MemberKey);

                // compare the timestamp of the new item with any existing member (or ABSENT witness)
                PresenceMessage existingItem;
                if (members.TryGetValue(item.MemberKey, out existingItem) && (item.Timestamp < existingItem.Timestamp))
                    return false;

                members[item.MemberKey] = item;

                return true;
            }

            public bool Remove(PresenceMessage item)
            {
                bool result = true;

                PresenceMessage existingItem;
                if (members.TryGetValue(item.MemberKey, out existingItem) &&
                    existingItem.Action == PresenceAction.Absent)
                {
                    result = false;
                }

                members.Remove(item.MemberKey);
                return result;
            }

            public void StartSync()
            {
                if(Logger.IsDebug) Logger.Debug($"StartSync | Channel: {_channelName}, SyncInProgress: {IsSyncInProgress}");
                if (!IsSyncInProgress)
                {
                    residualMembers = new HashSet<string>(members.Keys);
                    IsSyncInProgress = true;
                }
            }

            public void EndSync()
            {
                if (Logger.IsDebug) Logger.Debug($"EndSync | Channel: {_channelName}, SyncInProgress: {IsSyncInProgress}");

                if (!IsSyncInProgress)
                    return;

                try
                {
                    // We can now strip out the ABSENT members, as we have
                    // received all of the out-of-order sync messages
                    foreach (var member in members.ToArray())
                        if (member.Value.Action == PresenceAction.Present)
                            members.Remove(member.Key);

                    // Any members that were present at the start of the sync,
                    // and have not been seen in sync, can be removed
                    foreach (var member in residualMembers)
                        members.Remove(member);
                    residualMembers = null;
                }
                finally
                {
                    IsSyncInProgress = false;
                }
            }
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