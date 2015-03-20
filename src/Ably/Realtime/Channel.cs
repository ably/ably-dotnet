using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably.Realtime
{
    public class Channel : IChannel
    {
        internal Channel(IConnectionManager connection)
        {
            this.connection = connection;
            this.connection.MessageReceived += OnConnectionMessageReceived;
        }

        private IConnectionManager connection;

        /// <summary>
        /// 
        /// </summary>
        public event MessageReceivedDelegate MessageReceived;

        public event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

        /// <summary>
        /// The channel name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Indicates the current state of this channel.
        /// </summary>
        public ChannelState State { get; private set; }

        /// <summary>
        /// Attach to this channel. Any resulting channel state change will be indicated to any registered 
        /// <see cref="ChannelStateChanged"/> listener.
        /// </summary>
        public void Attach()
        {
            if (this.State == ChannelState.Attaching || this.State == ChannelState.Attached)
            {
                return;
            }

            if (!this.connection.IsActive)
            {
                // TODO: Throw error with details
            }

            this.SetChannelState(ChannelState.Attaching);
            this.connection.Send(new ProtocolMessage(ProtocolMessage.Action.Attach, this.Name));
        }

        /// <summary>
        /// Detach from this channel. Any resulting channel state change will be indicated to any registered 
        /// <see cref="ChannelStateChanged"/> listener.
        /// </summary>
        public void Detach()
        {
            if (this.State == ChannelState.Initialised || this.State == ChannelState.Detaching ||
                this.State == ChannelState.Detached)
            {
                return;
            }

            if (!this.connection.IsActive)
            {
                // TODO: Throw error with details
            }

            this.SetChannelState(ChannelState.Detaching);
            this.connection.Send(new ProtocolMessage(ProtocolMessage.Action.Detach, this.Name));
        }

        /// <summary>
        /// Publish a single message on this channel based on a given event name and payload.
        /// </summary>
        /// <param name="name">The event name.</param>
        /// <param name="data">The payload of the message.</param>
        public void Publish(string name, object data)
        {
            this.Publish(new Message[] { new Message(name, data) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messages"></param>
        public void Publish(IEnumerable<Message> messages)
        {
            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.Action.Message);
            message.Messages = messages.ToArray();
            if (this.State == ChannelState.Initialised || this.State == ChannelState.Attaching)
            {
                // TODO: Queue messages
            }
            else if (this.State == ChannelState.Attached)
            {
                this.connection.Send(message);
            }
            else
            {
                throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000, System.Net.HttpStatusCode.BadRequest));
            }
        }

        public IPartialResult<Message> History()
        {
            // TODO: Implement
            throw new NotImplementedException();
        }

        public IPartialResult<Message> History(HistoryDataRequestQuery query)
        {
            // TODO: Implement
            throw new NotImplementedException();
        }

        public IList<PresenceMessage> Presence()
        {
            // TODO: Implement
            throw new NotImplementedException();
        }

        protected void SetChannelState(ChannelState state)
        {
            this.State = state;
            this.OnChannelStateChanged(new ChannelStateChangedEventArgs(state));
        }

        private void OnMessageReceived()
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived();
            }
        }

        private void OnChannelStateChanged(ChannelStateChangedEventArgs eventArgs)
        {
            if (this.ChannelStateChanged != null)
            {
                this.ChannelStateChanged(this, eventArgs);
            }
        }

        private void OnConnectionMessageReceived()
        {
            this.OnMessageReceived();
        }
    }
}
