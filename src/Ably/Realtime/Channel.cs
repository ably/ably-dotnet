using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably.Realtime
{
    public class Channel : IRealtimeChannel
    {
        internal Channel(string name, IConnectionManager connection)
        {
            this.queuedMessages = new List<Message>();
            this.eventListeners = new Dictionary<string, List<Action<Message[]>>>();
            this.Name = name;
            this.Presence = new Presence(connection, name);
            this.connection = connection;
            this.connection.MessageReceived += OnConnectionMessageReceived;
        }

        private IConnectionManager connection;
        private ILogger Logger = Config.AblyLogger;
        private List<Message> queuedMessages;
        private Dictionary<string, List<Action<Message[]>>> eventListeners;

        /// <summary>
        /// 
        /// </summary>
        public event Action<Message[]> MessageReceived;

        public event EventHandler<ChannelStateChangedEventArgs> ChannelStateChanged;

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
            if (this.State == ChannelState.Attaching || this.State == ChannelState.Attached)
            {
                return;
            }

            if (!this.connection.IsActive)
            {
                this.connection.Connect();
            }

            this.SetChannelState(ChannelState.Attaching);
            this.connection.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Attach, this.Name), null);
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

            if (this.State == ChannelState.Failed)
            {
                throw new AblyException("Channel is Failed");
            }

            this.SetChannelState(ChannelState.Detaching);
            this.connection.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Detach, this.Name), null);
        }

        public void Subscribe(string eventName, Action<Message[]> listener)
        {
            List<Action<Message[]>> messageDelegate;
            if (!this.eventListeners.TryGetValue(eventName, out messageDelegate))
            {
                messageDelegate = new List<Action<Message[]>>();
                this.eventListeners.Add(eventName, messageDelegate);
            }
            messageDelegate.Add(listener);
        }

        public void Unsubscribe(string eventName, Action<Message[]> listener)
        {
            List<Action<Message[]>> messageDelegate;
            if (this.eventListeners.TryGetValue(eventName, out messageDelegate))
            {
                messageDelegate.Remove(listener);
            }
        }

        /// <summary>
        /// Publish a single message on this channel based on a given event name and payload.
        /// </summary>
        /// <param name="name">The event name.</param>
        /// <param name="data">The payload of the message.</param>
        public void Publish(string name, object data)
        {
            this.Publish(name, data, null);
        }

        public void Publish(string name, object data, Action<bool, ErrorInfo> callback)
        {
            this.Publish(new Message[] { new Message(name, data) }, callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messages"></param>
        public void Publish(IEnumerable<Message> messages)
        {
            this.Publish(messages, null);
        }

        public void Publish(IEnumerable<Message> messages, Action<bool, ErrorInfo> callback)
        {
            if (this.State == ChannelState.Initialised || this.State == ChannelState.Attaching)
            {
                this.queuedMessages.AddRange(messages);
            }
            else if (this.State == ChannelState.Attached)
            {
                ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, this.Name);
                message.Messages = messages.ToArray();
                this.connection.Send(message, callback);
            }
            else
            {
                throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000, System.Net.HttpStatusCode.BadRequest));
            }
        }

        protected void SetChannelState(ChannelState state)
        {
            this.State = state;
            this.OnChannelStateChanged(new ChannelStateChangedEventArgs(state));
        }

        private void OnChannelStateChanged(ChannelStateChangedEventArgs eventArgs)
        {
            if (this.ChannelStateChanged != null)
            {
                this.ChannelStateChanged(this, eventArgs);
            }
        }

        private void OnConnectionMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Attached:
                    if (this.State == ChannelState.Attaching)
                    {
                        this.SetChannelState(ChannelState.Attached);
                        this.SendQueuedMessages();
                    }
                    break;
                case ProtocolMessage.MessageAction.Detached:
                    if (this.State == ChannelState.Detaching)
                        this.SetChannelState(ChannelState.Detached);
                    break;
                case ProtocolMessage.MessageAction.Message:
                    this.OnMessage(message);
                    break;
                case ProtocolMessage.MessageAction.Presence:
                    this.OnPresence(message, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    this.OnSync(message);
                    break;
                case ProtocolMessage.MessageAction.Error:
                    this.SetChannelState(ChannelState.Failed);
                    break;
                default:
                    Logger.Error("Channel::OnConnectionMessageReceived(): Unexpected message action {0}", message.Action);
                    break;
            }
        }

        private void OnMessage(ProtocolMessage message)
        {
            Message[] messages = message.Messages;
            for (int i = 0; i < messages.Length; i++)
            {
                Message msg = messages[i];
                // TODO: populate fields derived from protocol message
                List<Action<Message[]>> listeners = eventListeners.Get(msg.Name, null);
                if (listeners != null)
                {
                    Message[] singleMessage = new Message[] { msg };
                    foreach (var listener in listeners)
                    {
                        listener(singleMessage);
                    }
                }
            }
            if (this.MessageReceived != null)
            {
                this.MessageReceived(messages);
            }
        }

        private void OnPresence(ProtocolMessage message, string channelSerial)
        {
            // TODO: Implement Channel.OnPresence
        }

        private void OnSync(ProtocolMessage message)
        {
            // TODO: Implement Channel.OnSync
            this.OnPresence(message, "");
        }

        private void SendQueuedMessages()
        {
            if (this.queuedMessages.Count == 0)
                return;

            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Message, this.Name);
            message.Messages = this.queuedMessages.ToArray();
            this.queuedMessages.Clear();
            // TODO: Add callbacks
            this.connection.Send(message, null);
        }
    }
}
