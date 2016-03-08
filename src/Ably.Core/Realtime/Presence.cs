using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Transport;
using IO.Ably.Types;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    public class Presence
    {
        public Presence( IConnectionManager connection, IRealtimeChannel channel, string cliendId )
        {
            this.presence = new PresenceMap();
            this.pendingPresence = new List<QueuedPresenceMessage>();
            this.connection = connection;
            this.connection.MessageReceived += OnConnectionMessageReceived;
            this.channel = channel;
            this.channel.ChannelStateChanged += this.OnChannelStateChanged;
            this.clientId = cliendId;
        }

        private IConnectionManager connection;
        private PresenceMap presence;
        private IRealtimeChannel channel;
        private string clientId;
        private List<QueuedPresenceMessage> pendingPresence;

        public event Action<PresenceMessage[]> MessageReceived;
        // TODO: Subscribe with an action specifier

        public PresenceMessage[] Get()
        {
            return this.presence.Values;
        }

        public Task Enter( object clientData )
        {
            return this.EnterClient( this.clientId, clientData );
        }

        public Task EnterClient( string clientId, object clientData )
        {
            return this.UpdatePresence( new PresenceMessage( PresenceMessage.ActionType.Enter, clientId, clientData ) );
        }

        public Task Update( object clientData )
        {
            return this.UpdateClient( this.clientId, clientData );
        }

        public Task UpdateClient( string clientId, object clientData )
        {
            return this.UpdatePresence( new PresenceMessage( PresenceMessage.ActionType.Update, clientId, clientData ) );
        }

        public Task Leave( object clientData )
        {
            return this.LeaveClient( this.clientId, clientData );
        }

        public Task Leave()
        {
            return this.LeaveClient( this.clientId, null );
        }

        public Task LeaveClient( string clientId, object clientData )
        {
            return this.UpdatePresence( new PresenceMessage( PresenceMessage.ActionType.Leave, clientId, clientData ) );
        }

        private Task UpdatePresence( PresenceMessage msg )
        {
            if (this.channel.State == ChannelState.Initialized || this.channel.State == ChannelState.Attaching)
            {
                if (this.channel.State == ChannelState.Initialized)
                {
                    this.channel.Attach();
                }
                TaskWrapper tw = new TaskWrapper();
                this.pendingPresence.Add( new QueuedPresenceMessage( msg, tw.callback ) );
                return tw;
            }
            if( this.channel.State == ChannelState.Attached )
            {
                ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, this.channel.Name);
                message.presence = new PresenceMessage[] { msg };
                return this.connection.SendAsync( message );
            }
            throw new AblyException( "Unable to enter presence channel in detached or failed state", 91001, System.Net.HttpStatusCode.BadRequest );
        }

        private void OnConnectionMessageReceived( ProtocolMessage message )
        {
            switch( message.action )
            {
                case ProtocolMessage.MessageAction.Presence:
                    this.OnPresence( message.presence, null );
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    this.OnPresence( message.presence, message.channelSerial );
                    break;
            }
        }

        private void OnPresence( PresenceMessage[] messages, string syncChannelSerial )
        {
            string syncCursor = null;
            bool broadcast = true;
            if( syncChannelSerial != null )
            {
                syncCursor = syncChannelSerial.Substring( syncChannelSerial.IndexOf( ':' ) );
                if( syncCursor.Length > 1 )
                {
                    presence.StartSync();
                }
            }
            foreach( PresenceMessage update in messages )
            {
                switch( update.action )
                {
                    case PresenceMessage.ActionType.Enter:
                    case PresenceMessage.ActionType.Update:
                    case PresenceMessage.ActionType.Present:
                        broadcast &= presence.Put( update );
                        break;
                    case PresenceMessage.ActionType.Leave:
                        broadcast &= presence.Remove( update );
                        break;
                }
            }
            // if this is the last message in a sequence of sync updates, end the sync
            if( syncChannelSerial == null || syncCursor.Length <= 1 )
            {
                presence.EndSync();
            }

            if( broadcast )
            {
                this.Publish( messages );
            }
        }

        private void Publish( params PresenceMessage[] messages )
        {
            if( this.MessageReceived != null )
            {
                this.MessageReceived( messages );
            }
        }

        private void OnChannelStateChanged( object sender, ChannelStateChangedEventArgs e )
        {
            if( e.NewState == ChannelState.Attached )
            {
                this.SendQueuedMessages();
            }
            else if( e.NewState == ChannelState.Detached || e.NewState == ChannelState.Failed )
            {
                this.FailQueuedMessages( e.Reason );
            }
        }

        private void SendQueuedMessages()
        {
            if( this.pendingPresence.Count == 0 )
                return;

            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, this.channel.Name);
            message.presence = new PresenceMessage[ this.pendingPresence.Count ];
            List<Action<bool, ErrorInfo>> callbacks = new List<Action<bool, ErrorInfo>>();
            int i = 0;
            foreach( QueuedPresenceMessage presenceMessage in this.pendingPresence )
            {
                message.presence[ i++ ] = presenceMessage.Message;
                if( presenceMessage.Callback != null )
                {
                    callbacks.Add( presenceMessage.Callback );
                }
            }
            this.pendingPresence.Clear();

            this.connection.Send( message, ( s, e ) =>
            {
                foreach( var callback in callbacks )
                {
                    callback( s, e );
                }
            } );
        }

        private void FailQueuedMessages( ErrorInfo reason )
        {
            foreach( QueuedPresenceMessage presenceMessage in this.pendingPresence.Where( c => c.Callback != null ) )
            {
                presenceMessage.Callback( false, reason );
            }
            this.pendingPresence.Clear();
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
                    return this.members.Values.Where( c => c.action != PresenceMessage.ActionType.Absent )
                        .ToArray();
                }
            }

            public bool Put( PresenceMessage item )
            {
                string key = MemberKey(item);

                // we've seen this member, so do not remove it at the end of sync
                if( residualMembers != null )
                {
                    residualMembers.Remove( key );
                }

                // compare the timestamp of the new item with any existing member (or ABSENT witness)
                PresenceMessage existingItem;
                if( members.TryGetValue( key, out existingItem ) && item.timestamp < existingItem.timestamp )
                {
                    // no item supersedes a newer item with the same key
                    return false;
                }

                // add or update
                if( !members.ContainsKey( key ) )
                {
                    members.Add( key, item );
                }
                else
                {
                    members[ key ] = item;
                }

                return true;
            }

            public bool Remove( PresenceMessage item )
            {
                string key = MemberKey(item);
                PresenceMessage existingItem;
                if( members.TryGetValue( key, out existingItem ) && existingItem.action == PresenceMessage.ActionType.Absent )
                {
                    return false;
                }

                members.Remove( key );
                return true;
            }

            public void StartSync()
            {
                if( !this.isSyncInProgress )
                {
                    residualMembers = new HashSet<string>( members.Keys );
                    this.isSyncInProgress = true;
                }
            }

            public void EndSync()
            {
                if( !this.isSyncInProgress )
                {
                    return;
                }

                try
                {
                    // We can now strip out the ABSENT members, as we have
                    // received all of the out-of-order sync messages
                    foreach( KeyValuePair<string, PresenceMessage> member in this.members.ToArray() )
                    {
                        if( member.Value.action == PresenceMessage.ActionType.Present )
                        {
                            this.members.Remove( member.Key );
                        }
                    }

                    // Any members that were present at the start of the sync,
                    // and have not been seen in sync, can be removed
                    foreach( string member in this.residualMembers )
                    {
                        this.members.Remove( member );
                    }
                    residualMembers = null;
                }
                finally
                {
                    this.isSyncInProgress = false;
                }
            }

            private string MemberKey( PresenceMessage message )
            {
                return string.Format( "{0}:{1}", message.connectionId, message.clientId );
            }
        }
    }

    internal class QueuedPresenceMessage
    {
        public QueuedPresenceMessage( PresenceMessage message, Action<bool, ErrorInfo> callback )
        {
            this.Message = message;
            this.Callback = callback;
        }

        public PresenceMessage Message { get; private set; }
        public Action<bool, ErrorInfo> Callback { get; private set; }
    }
}
