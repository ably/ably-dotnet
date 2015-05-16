using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably.Realtime
{
    public interface IPresenceFactory
    {
        Presence Create(string channel);
    }

    public class PresenceFactory : IPresenceFactory
    {
        public IConnectionManager ConnectionManager { get; set; }
        public AblyRealtimeOptions Options { get; set; }

        public Presence Create(string channel)
        {
            return new Presence(this.ConnectionManager, channel, this.Options.ClientId);
        }
    }

    public class Presence
    {
        public Presence(IConnectionManager connection, string channel, string cliendId)
        {
            this.presence = new PresenceMap();
            this.connection = connection;
            this.connection.MessageReceived += OnConnectionMessageReceived;
            this.channel = channel;
            this.clientId = cliendId;
        }

        private IConnectionManager connection;
        private PresenceMap presence;
        private string channel;
        private string clientId;

        public event Action<PresenceMessage[]> MessageReceived;
        // TODO: Subscribe with an action specifier

        public PresenceMessage[] Get()
        {
            return this.presence.Values;
        }

        public void Enter(object clientData, Action<bool, ErrorInfo> callback)
        {
            this.EnterClient(this.clientId, clientData, callback);
        }

        public void EnterClient(string clientId, object clientData, Action<bool, ErrorInfo> callback)
        {
            this.UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Enter, clientId, clientData), callback);
        }

        public void Update(object clientData, Action<bool, ErrorInfo> callback)
        {
            this.UpdateClient(this.clientId, clientData, callback);
        }

        public void UpdateClient(string clientId, object clientData, Action<bool, ErrorInfo> callback)
        {
            this.UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Update, clientId, clientData), callback);
        }

        public void Leave(object clientData, Action<bool, ErrorInfo> callback)
        {
            this.LeaveClient(this.clientId, clientData, callback);
        }

        public void Leave(Action<bool, ErrorInfo> callback)
        {
            this.LeaveClient(this.clientId, null, callback);
        }

        public void LeaveClient(string clientId, object clientData, Action<bool, ErrorInfo> callback)
        {
            this.UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Leave, clientId, clientData), callback);
        }

        private void UpdatePresence(PresenceMessage msg, Action<bool, ErrorInfo> callback)
        {
            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, this.channel);
            message.Presence = new PresenceMessage[] { msg };
            this.connection.Send(message, callback);
        }

        private void OnConnectionMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Presence:
                    this.OnPresence(message.Presence, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    this.OnSync(message);
                    break;
            }
        }

        private void OnPresence(PresenceMessage[] messages, string syncChannelSerial)
        {
            string syncCursor = null;
            bool broadcast = true;
            if (syncChannelSerial != null)
            {
                syncCursor = syncChannelSerial.Substring(syncChannelSerial.IndexOf(':'));
                if (syncCursor.Length > 1)
                {
                    presence.StartSync();
                }
            }
            foreach (PresenceMessage update in messages)
            {
                switch (update.Action)
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
                this.Publish(messages);
            }
        }

        private void OnSync(ProtocolMessage message)
        {
            this.OnPresence(message.Presence, message.ChannelSerial);
        }

        private void Publish(params PresenceMessage[] messages)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(messages);
            }
        }

        class PresenceMap
        {
            public PresenceMap()
            {
                this.members = new Dictionary<string, PresenceMessage>();
            }

            private Dictionary<string, PresenceMessage> members;
            private bool isSyncInProgress;
            private ICollection<string> residualMembers;

            public PresenceMessage[] Values
            {
                get
                {
                    return this.members.Values.Where(c => c.Action != PresenceMessage.ActionType.Absent)
                        .ToArray();
                }
            }

            public bool Put(PresenceMessage item)
            {
                string key = MemberKey(item);

                // compare the timestamp of the new item with any existing member (or ABSENT witness)
                PresenceMessage existingItem;
                if (members.TryGetValue(key, out existingItem) && item.Timestamp < existingItem.Timestamp)
                {
                    // no item supersedes a newer item with the same key
                    return false;
                }

                members.Add(key, item);
                return true;
            }

            public bool Remove(PresenceMessage item)
            {
                string key = MemberKey(item);
                PresenceMessage existingItem;
                if (members.TryGetValue(key, out existingItem) && existingItem.Action == PresenceMessage.ActionType.Absent)
                {
                    return false;
                }

                members.Remove(key);
                return true;
            }

            public void StartSync()
            {
                if (!this.isSyncInProgress)
                {
                    residualMembers = new HashSet<string>(members.Keys);
                    this.isSyncInProgress = true;
                }
            }

            public void EndSync()
            {
                if (!this.isSyncInProgress)
                {
                    return;
                }

                try
                {
                    // We can now strip out the ABSENT members, as we have
                    // received all of the out-of-order sync messages
                    foreach (KeyValuePair<string, PresenceMessage> member in this.members.ToArray())
                    {
                        if (member.Value.Action == PresenceMessage.ActionType.Present)
                        {
                            this.members.Remove(member.Key);
                        }
                    }

                    // Any members that were present at the start of the sync,
                    // and have not been seen in sync, can be removed
                    foreach (string member in this.residualMembers)
                    {
                        this.members.Remove(member);
                    }
                    residualMembers = null;
                }
                finally
                {
                    this.isSyncInProgress = false;
                }
            }

            private string MemberKey(PresenceMessage message)
            {
                return string.Format("{0}:{1}", message.ConnectionId, message.ClientId);
            }
        }
    }
}
