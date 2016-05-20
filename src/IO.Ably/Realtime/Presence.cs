using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public enum PresenceState
    {
        Initialized,
        Entering,
        Entered,
        Leaving,
        Left,
        Failed
    }


    public class Presence
    {
        private readonly IRealtimeChannel channel;
        private readonly string clientId;

        private readonly IConnectionManager connection;
        private readonly List<QueuedPresenceMessage> pendingPresence;
        private readonly PresenceMap presence;

        internal Presence(IConnectionManager connection, IRealtimeChannel channel, string cliendId)
        {
            presence = new PresenceMap();
            pendingPresence = new List<QueuedPresenceMessage>();
            this.connection = connection;
            this.channel = channel;
            this.channel.StateChanged += OnChannelStateChanged;
            clientId = cliendId;
        }

        public event Action<PresenceMessage[]> MessageReceived;

        public PresenceMessage[] Get()
        {
            return presence.Values;
        }

        public Task Enter(object clientData)
        {
            return EnterClient(clientId, clientData);
        }

        public Task EnterClient(string clientId, object clientData)
        {
            return UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Enter, clientId, clientData));
        }

        public Task Update(object clientData)
        {
            return UpdateClient(clientId, clientData);
        }

        public Task UpdateClient(string clientId, object clientData)
        {
            return UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Update, clientId, clientData));
        }

        public Task Leave(object clientData)
        {
            return LeaveClient(clientId, clientData);
        }

        public Task Leave()
        {
            return LeaveClient(clientId, null);
        }

        public Task LeaveClient(string clientId, object clientData)
        {
            return UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Leave, clientId, clientData));
        }

        internal Task UpdatePresence(PresenceMessage msg)
        {
            if (channel.State == ChannelState.Initialized || channel.State == ChannelState.Attaching)
            {
                if (channel.State == ChannelState.Initialized)
                {
                    channel.Attach();
                }

                var tw = new TaskWrapper();
                pendingPresence.Add(new QueuedPresenceMessage(msg, tw.Callback));
                return tw.Task;
            }
            if (channel.State == ChannelState.Attached)
            {
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, channel.Name);
                message.presence = new[] {msg};
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
                {
                    presence.StartSync();
                }
            }
            foreach (var update in messages)
            {
                switch (update.action)
                {
                    case PresenceMessage.ActionType.Enter:
                    case PresenceMessage.ActionType.Update:
                    case PresenceMessage.ActionType.Present:
                        broadcast &= presence.Put(update);
                        break;
                    case PresenceMessage.ActionType.Leave:
                        broadcast &= presence.Remove(update);
                        break;
                }
            }
            // if this is the last message in a sequence of sync updates, end the sync
            if (syncChannelSerial == null || syncCursor.Length <= 1)
            {
                presence.EndSync();
            }

            if (broadcast)
            {
                Publish(messages);
            }
        }

        private void Publish(params PresenceMessage[] messages)
        {
            if (MessageReceived != null)
            {
                MessageReceived(messages);
            }
        }

        private void OnChannelStateChanged(object sender, ChannelStateChangedEventArgs e)
        {
            if (e.NewState == ChannelState.Attached)
            {
                SendQueuedMessages();
            }
            else if (e.NewState == ChannelState.Detached || e.NewState == ChannelState.Failed)
            {
                FailQueuedMessages(e.Reason);
            }
        }

        private void SendQueuedMessages()
        {
            if (pendingPresence.Count == 0)
                return;

            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, channel.Name);
            message.presence = new PresenceMessage[pendingPresence.Count];
            var callbacks = new List<Action<bool, ErrorInfo>>();
            var i = 0;
            foreach (var presenceMessage in pendingPresence)
            {
                message.presence[i++] = presenceMessage.Message;
                if (presenceMessage.Callback != null)
                {
                    callbacks.Add(presenceMessage.Callback);
                }
            }
            pendingPresence.Clear();

            connection.Send(message, (s, e) =>
            {
                foreach (var callback in callbacks)
                {
                    callback(s, e);
                }
            });
        }

        private void FailQueuedMessages(ErrorInfo reason)
        {
            foreach (var presenceMessage in pendingPresence.Where(c => c.Callback != null))
            {
                presenceMessage.Callback(false, reason);
            }
            pendingPresence.Clear();
        }

        private class PresenceMap
        {
            public enum State
            {
                Initialized,
                SyncStarting,
                InSync,
                Failed
            }

            private bool isSyncInProgress;

            private readonly Dictionary<string, PresenceMessage> members;
            private ICollection<string> residualMembers;

            public PresenceMap()
            {
                members = new Dictionary<string, PresenceMessage>();
            }

            public PresenceMessage[] Values
            {
                get
                {
                    return members.Values.Where(c => c.action != PresenceMessage.ActionType.Absent)
                        .ToArray();
                }
            }

            public bool Put(PresenceMessage item)
            {
                var key = MemberKey(item);

                // we've seen this member, so do not remove it at the end of sync
                if (residualMembers != null)
                {
                    residualMembers.Remove(key);
                }

                // compare the timestamp of the new item with any existing member (or ABSENT witness)
                PresenceMessage existingItem;
                if (members.TryGetValue(key, out existingItem) && item.timestamp < existingItem.timestamp)
                {
                    // no item supersedes a newer item with the same key
                    return false;
                }

                // add or update
                if (!members.ContainsKey(key))
                {
                    members.Add(key, item);
                }
                else
                {
                    members[key] = item;
                }

                return true;
            }

            public bool Remove(PresenceMessage item)
            {
                var key = MemberKey(item);
                PresenceMessage existingItem;
                if (members.TryGetValue(key, out existingItem) &&
                    existingItem.action == PresenceMessage.ActionType.Absent)
                {
                    return false;
                }

                members.Remove(key);
                return true;
            }

            public void StartSync()
            {
                if (!isSyncInProgress)
                {
                    residualMembers = new HashSet<string>(members.Keys);
                    isSyncInProgress = true;
                }
            }

            public void EndSync()
            {
                if (!isSyncInProgress)
                {
                    return;
                }

                try
                {
                    // We can now strip out the ABSENT members, as we have
                    // received all of the out-of-order sync messages
                    foreach (var member in members.ToArray())
                    {
                        if (member.Value.action == PresenceMessage.ActionType.Present)
                        {
                            members.Remove(member.Key);
                        }
                    }

                    // Any members that were present at the start of the sync,
                    // and have not been seen in sync, can be removed
                    foreach (var member in residualMembers)
                    {
                        members.Remove(member);
                    }
                    residualMembers = null;
                }
                finally
                {
                    isSyncInProgress = false;
                }
            }

            private string MemberKey(PresenceMessage message)
            {
                return $"{message.connectionId}:{message.clientId}";
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