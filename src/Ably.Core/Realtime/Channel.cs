using IO.Ably.Transport;
using IO.Ably.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    internal class Channel : IRealtimeChannel
    {
        internal Channel( string name, string clientId, IConnectionManager connection )
        {
            this.Name = name;
            this.Presence = new Presence( connection, this, clientId );
            this.connection = connection;
            this.connection.MessageReceived += OnConnectionMessageReceived;
        }

        readonly IConnectionManager connection;
        readonly List<Message> queuedMessages = new List<Message>(16);
        readonly Handlers handlersAll = new Handlers();
        readonly Dictionary<string, Handlers> handlersSpecific = new Dictionary<string, Handlers>();

        /// <summary>A lock object to protect subscribers. Only locked while subscribing, unsubscribing, or receiving those messages.</summary>
        readonly object syncRoot = new object();

        public event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        public Rest.ChannelOptions Options { get; set; }

        /// <summary>
        /// The channel name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Indicates the current state of this channel.
        /// </summary>
        public ChannelState State { get; private set; }

        public Presence Presence { get; private set; }

        /// <summary>
        /// Attach to this channel. Any resulting channel state change will be indicated to any registered
        /// <see cref="ChannelStateChanged"/> listener.
        /// </summary>
        public void Attach()
        {
            if( this.State == ChannelState.Attaching || this.State == ChannelState.Attached )
            {
                return;
            }

            if( !this.connection.IsActive )
            {
                this.connection.Connect();
            }

            this.SetChannelState( ChannelState.Attaching );
            this.connection.Send( new ProtocolMessage( ProtocolMessage.MessageAction.Attach, this.Name ), null );
        }

        /// <summary>
        /// Detach from this channel. Any resulting channel state change will be indicated to any registered
        /// <see cref="ChannelStateChanged"/> listener.
        /// </summary>
        public void Detach()
        {
            if (this.State == ChannelState.Initialized || this.State == ChannelState.Detaching ||
                this.State == ChannelState.Detached)
            {
                return;
            }

            if( this.State == ChannelState.Failed )
            {
                throw new AblyException( "Channel is Failed" );
            }

            this.SetChannelState( ChannelState.Detaching );
            this.connection.Send( new ProtocolMessage( ProtocolMessage.MessageAction.Detach, this.Name ), null );
        }

        public void Subscribe( IMessageHandler handler )
        {
            lock ( syncRoot )
                handlersAll.add( handler );
        }

        public void Subscribe( string eventName, IMessageHandler handler )
        {
            lock ( syncRoot )
            {
                Handlers handlers;
                if( !this.handlersSpecific.TryGetValue( eventName, out handlers ) )
                {
                    handlers = new Handlers();
                    this.handlersSpecific.Add( eventName, handlers );
                }
                handlers.add( handler );
            }
        }

        public void Unsubscribe( IMessageHandler handler )
        {
            lock ( syncRoot )
            {
                if( handlersAll.remove( handler ) )
                    return;
            }
            Logger.Warning( "Unsubscribe failed: was not subscribed" );
        }

        public void Unsubscribe( string eventName, IMessageHandler handler )
        {
            lock ( syncRoot )
            {
                Handlers handlers;
                if( this.handlersSpecific.TryGetValue( eventName, out handlers ) )
                    if( handlers.remove( handler ) )
                        return;
            }
            Logger.Warning( "Unsubscribe failed: was not subscribed" );
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        /// <param name="name">The event name.</param>
        /// <param name="data">The payload of the message.</param>
        public void Publish( string name, object data )
        {
            this.PublishAsync( name, data ).IgnoreExceptions();
        }

        /// <summary>Publish a single message on this channel based on a given event name and payload.</summary>
        public Task PublishAsync( string name, object data )
        {
            return this.PublishAsync( new Message[] { new Message( name, data ) } );
        }

        /// <summary>Publish several messages on this channel.</summary>
        public void Publish( IEnumerable<Message> messages )
        {
            this.PublishAsync( messages ).IgnoreExceptions();
        }

        /// <summary>Publish several messages on this channel.</summary>
        public Task PublishAsync( IEnumerable<Message> messages )
        {
<<<<<<< HEAD
            if (this.State == ChannelState.Initialized || this.State == ChannelState.Attaching)
=======
            if( this.State == ChannelState.Initialised || this.State == ChannelState.Attaching )
>>>>>>> Asynchronous realtime (untested)
            {
                this.queuedMessages.AddRange( messages );
                // TODO: implement callback
                return Task<bool>.FromResult( true );
            }

            if( this.State == ChannelState.Attached )
            {
                ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, this.Name);
                message.messages = messages.ToArray();
                TaskWrapper tw = new TaskWrapper();
                this.connection.Send( message, tw.callback );
                return tw;
            }
            throw new AblyException( new ErrorInfo( "Unable to publish in detached or failed state", 40000, System.Net.HttpStatusCode.BadRequest ) );
        }

        protected void SetChannelState( ChannelState state )
        {
            this.State = state;
            this.OnChannelStateChanged( new ChannelStateChangedEventArgs( state ) );
        }

        void OnChannelStateChanged( ChannelStateChangedEventArgs eventArgs )
        {
            if( this.ChannelStateChanged != null )
            {
                this.ChannelStateChanged( this, eventArgs );
            }
        }

        void OnConnectionMessageReceived( ProtocolMessage message )
        {
            switch( message.action )
            {
                case ProtocolMessage.MessageAction.Attached:
                    if( this.State == ChannelState.Attaching )
                    {
                        this.SetChannelState( ChannelState.Attached );
                        this.SendQueuedMessages();
                    }
                    break;
                case ProtocolMessage.MessageAction.Detached:
                    if( this.State == ChannelState.Detaching )
                        this.SetChannelState( ChannelState.Detached );
                    break;
                case ProtocolMessage.MessageAction.Message:
                    this.OnMessage( message );
                    break;
                case ProtocolMessage.MessageAction.Error:
                    this.SetChannelState( ChannelState.Failed );
                    break;
                default:
                    Logger.Error( "Channel::OnConnectionMessageReceived(): Unexpected message action {0}", message.action );
                    break;
            }
        }

        private void OnMessage( ProtocolMessage message )
        {
            foreach( Message msg in message.messages )
            {
                lock ( syncRoot )
                {
                    foreach( IMessageHandler h in handlersAll.alive() )
                        h.Handle( msg );

                    Handlers handlers;
                    if( !handlersSpecific.TryGetValue( msg.name, out handlers ) )
                        continue;
                    foreach( IMessageHandler h in handlers.alive() )
                        h.Handle( msg );
                }
            }
        }

        private void SendQueuedMessages()
        {
            if( this.queuedMessages.Count == 0 )
                return;

            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, this.Name);
            message.messages = this.queuedMessages.ToArray();
            this.queuedMessages.Clear();
            // TODO: Add callbacks
            this.connection.Send( message, null );
        }
    }

    internal class QueuedProtocolMessage
    {
        public QueuedProtocolMessage( ProtocolMessage message, Action<bool, ErrorInfo> callback )
        {
            this.Message = message;
            this.Callback = callback;
        }

        public ProtocolMessage Message { get; private set; }
        public Action<bool, ErrorInfo> Callback { get; private set; }
    }
}