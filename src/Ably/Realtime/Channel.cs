using System.Net;
using System.Runtime.Remoting.Messaging;
using Ably.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ably.Realtime
{
    public class Channel
    {

        /************************************
         * ChannelState and state management
         ************************************/

        /**
         * Channel states. See Ably Realtime API documentation for more details.
         */
        public enum ChannelState
        {
            Initialised,
            Attaching,
            Attached,
            Detaching,
            Detached,
            Failed
        }

        /**
         * The name of this channel.
         */
        private string _name;

        /**
         * The {@link Presence} object for this channel. This controls this client's
         * presence on the channel and may also be used to obtain presence information
         * and change events for other members of the channel.
         */
        public Presence Presence { get; set; }

        /**
	 * The current channel state.
	 */
        public ChannelState State { get; set; }

        /**
	 * Error information associated with a failed channel state.
	 */
        public ErrorInfo Reason { get; set; }

        /**
	 * A message identifier indicating the time of attachment to the channel;
	 * used when recovering a message history to mesh exactly with messages
	 * received on this channel subsequent to attachment.
	 */
        public string AttachSerial { get; set; }

        /**
	 * An interface whereby a client may be notified of state changes for a channel.
	 */
        public interface IChannelStateListener
        {
            void OnChannelStateChanged(ChannelState state, ErrorInfo reason);
        }

        /**
         * A collection of listeners to be notified of state changes for this channel.
         */
        public StateMulticaster stateListeners = new StateMulticaster();

        public class StateMulticaster : List<IChannelStateListener>, IChannelStateListener
        {
            public void OnChannelStateChanged(ChannelState state, ErrorInfo reason)
            {
                foreach (var member in this)
                    try
                    {
                        member.OnChannelStateChanged(state, reason);
                    }
                    catch (Exception)
                    { }
            }
        }

        /***
         * internal
         *
         */
        private void SetState(ChannelState newState, ErrorInfo reason)
        {
            Logger.Current.Debug("setState(): channel = " + _name + "; setting " + newState);
            this.State = newState;
            this.Reason = reason;

            /* broadcast state change */
            stateListeners.OnChannelStateChanged(newState, reason);
        }

        /************************************
         * attach / detach
         ************************************/

        /**
         * Attach to this channel.
         * This call initiates the attach request, and the response
         * is indicated asynchronously in the resulting state change.
         * attach() is called implicitly when publishing or subscribing
         * on this channel, so it is not usually necessary for a client
         * to call attach() explicitly.
         * @throws AblyException
         */
        public void Attach()
        {
            Logger.Current.Debug("attach(); channel = " + _name);
            /* check preconditions */
            switch (State)
            {
                case ChannelState.Attached:
                case ChannelState.Attaching:
                    /* nothing to do */
                    return;
            }
            ConnectionManager connectionManager = _ably.Connection.ConnectionManager;
            if (!connectionManager.Active)
                throw new AblyException(connectionManager.StateErrorInfo);

            /* send attach request and pending state */
            var attachMessage = new ProtocolMessage(TAction.ATTACH, this._name);
            try
            {
                SetState(ChannelState.Attaching, null);
                connectionManager.Send(attachMessage, true, null);
            }
            catch (AblyException e)
            {
                throw e;
            }
        }

        /**
         * Detach from this channel.
         * This call initiates the detach request, and the response
         * is indicated asynchronously in the resulting state change.
         * @throws AblyException
         */
        public void Detach()
        {
            Logger.Current.Debug("detach(); channel = " + _name);
            /* check preconditions */
            switch (State)
            {
                case ChannelState.Initialised:
                case ChannelState.Detaching:
                case ChannelState.Detached:
                    /* nothing to do */
                    return;
            }
            ConnectionManager connectionManager = _ably.Connection.ConnectionManager;
            if (!connectionManager.Active)
                throw new AblyException(connectionManager.StateErrorInfo);

            /* send detach request */
            ProtocolMessage detachMessage = new ProtocolMessage(TAction.DETACH, this._name);
            try
            {
                SetState(ChannelState.Detaching, null);
                connectionManager.Send(detachMessage, true, null);
            }
            catch (AblyException e)
            {
                throw e;
            }
        }

        /***
         * internal
         *
         */
        private void SetAttached(ProtocolMessage message)
        {
            Logger.Current.Debug("setAttached(); channel = " + _name);
            AttachSerial = message.ChannelSerial;
            SetState(ChannelState.Attached, message.Error);
            SendQueuedMessages();
            Presence.SetAttached(message.Presence);
        }

        private void SetDetached(ProtocolMessage message)
        {
            Logger.Current.Debug("setDetached(); channel = " + _name);
            ErrorInfo reason = message.Error ?? ReasonNotAttached;
            SetState(ChannelState.Detached, reason);
            FailQueuedMessages(reason);
            Presence.SetDetached(reason);
        }

        private void SetFailed(ProtocolMessage message)
        {
            Logger.Current.Debug("setFailed(); channel = " + _name);
            ErrorInfo reason = message.Error;
            SetState(ChannelState.Failed, reason);
            FailQueuedMessages(reason);
            Presence.SetDetached(reason);
        }

        public void SetSuspended(ErrorInfo reason)
        {
            Logger.Current.Debug("setSuspended(); channel = " + _name);
            SetState(ChannelState.Detached, reason);
            FailQueuedMessages(reason);
            Presence.SetSuspended(reason);
        }

        static readonly ErrorInfo ReasonNotAttached = new ErrorInfo("Channel not attached", 90001, HttpStatusCode.BadRequest);

        /************************************
         * subscriptions and MessageListener
         ************************************/

        /**
         * An interface whereby a client maybe notified of messages changes on a channel.
         */
        public interface IMessageListener
        {
            void OnMessage(IEnumerable<Message> messages);
        }

        /**
         * Subscribe for messages on this channel. This implicitly attaches the channel if
         * not already attached.
         * @param listener: the MessageListener
         * @throws AblyException
         */
        public void Subscribe(IMessageListener listener)
        {
            Logger.Current.Debug("subscribe(); channel = " + this._name);
            listeners.Add(listener);
            Attach();
        }

        /**
         * Unsubscribe a previously subscribed listener from this channel.
         * @param listener: the previously subscribed listener.
         */
        public void Unsubscribe(IMessageListener listener)
        {
            Logger.Current.Debug("unsubscribe(); channel = " + this._name);
            listeners.Remove(listener);
        }

        /**
         * Subscribe for messages with a specific event name on this channel.
         * This implicitly attaches the channel if not already attached.
         * @param name: the event name
         * @param listener: the MessageListener
         * @throws AblyException
         */
        public void subscribe(String name, IMessageListener listener)
        {
            Logger.Current.Debug("subscribe(); channel = " + this._name + "; event = " + name);
            SubscribeImpl(name, listener);
            Attach();
        }

        /**
         * Unsubscribe a previously subscribed event listener from this channel.
         * @param name: the event name
         * @param listener: the previously subscribed listener.
         */
        public void Unsubscribe(string name, IMessageListener listener)
        {
            Logger.Current.Debug("unsubscribe(); channel = " + this._name + "; event = " + name);
            UnsubscribeImpl(name, listener);
        }

        /**
         * Subscribe for messages with an array of event names on this channel.
         * This implicitly attaches the channel if not already attached.
         * @param names: the event names
         * @param listener: the MessageListener
         * @throws AblyException
         */
        public void Subscribe(String[] names, IMessageListener listener)
        {
            Logger.Current.Debug("subscribe(); channel = " + this._name + "; (multiple events)");
            foreach (string name in names)
                SubscribeImpl(name, listener);
            Attach();
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /**
	 * Unsubscribe a previously subscribed event listener from this channel.
	 * @param names: the event names
	 * @param listener: the previously subscribed listener.
	 */
        public void Unsubscribe(String[] names, IMessageListener listener)
        {
            Logger.Current.Debug("unsubscribe(); channel = " + this._name + "; (multiple events)");
            foreach (var item in names)
                UnsubscribeImpl(item, listener);
        }

        /***
         * internal
         *
         */
        private void OnMessage(ProtocolMessage message)
        {
            Logger.Current.Debug("onMessage(); channel = " + _name);
            ProcessMessage(message.Messages);
        }

        private void OnPresence(ProtocolMessage message)
        {
            Logger.Current.Debug("onPresence(); channel = " + _name);
            Presence.SetPresence(message.Presence, true);
        }

        private void ProcessMessage(IEnumerable<Message> messages)
        {
            bool encrypted = _options != null && _options.Encrypted;
            foreach (var msg in messages)
            {
                if (encrypted)
                {
                    try
                    {
                        msg.DecryptData(_cipher);
                    }
                    catch (AblyException e)
                    {
                        Logger.Current.Error("Unexpected exception decrypting message", e);
                    }
                }
                Message[] singleMessage = new Message[] { msg };
                MessageMulticaster listeners = eventListeners.Get(msg.Name);
                if (listeners != null)
                    listeners.OnMessage(singleMessage);
            }
            this.listeners.OnMessage(messages);
        }

        private MessageMulticaster listeners = new MessageMulticaster();
        private Dictionary<String, MessageMulticaster> eventListeners = new Dictionary<String, MessageMulticaster>();

        private class MessageMulticaster : List<IMessageListener>, IMessageListener
        {

            public void OnMessage(IEnumerable<Message> messages)
            {
                foreach (var member in this)
                    try
                    {
                        member.OnMessage(messages);
                    }
                    catch (Exception ex) { Logger.Current.Error("OnMessage", ex); }
            }
        }

        private void SubscribeImpl(String name, IMessageListener listener)
        {
            MessageMulticaster listeners = eventListeners.Get(name);
            if (listeners == null)
            {
                listeners = new MessageMulticaster();
                eventListeners.Add(name, listeners);
            }
            listeners.Add(listener);
        }

        private void UnsubscribeImpl(string name, IMessageListener listener)
        {
            MessageMulticaster messageMulticaster = eventListeners.Get(name);
            if (messageMulticaster != null)
            {
                messageMulticaster.Remove(listener);
                if (messageMulticaster.Any() == false)
                    eventListeners.Remove(name);
            }
        }

        /************************************
         * publish and pending messages
         ************************************/

        /**
         * Publish a message on this channel. This implicitly attaches the channel if
         * not already attached.
         * @param name: the event name
         * @param data: the message payload. See {@link io.ably.types.Data} for supported datatypes
         * @param listener: a listener to be notified of the outcome of this message.
         * @throws AblyException
         */
        public void publish(String name, Object data, ICompletionListener listener)
        {
            Logger.Current.Debug("publish(String, Object); channel = " + this._name + "; event = " + name);
            publish(new[] { new Message(name, data) }, listener);
        }

        /**
         * Publish a message on this channel. This implicitly attaches the channel if
         * not already attached.
         * @param message: the message
         * @param listener: a listener to be notified of the outcome of this message.
         * @throws AblyException
         */
        public void publish(Message message, ICompletionListener listener)
        {
            Logger.Current.Debug("publish(Message); channel = " + this._name + "; event = " + message.Name);
            publish(new Message[] { message }, listener);
        }

        /**
         * Publish an array of messages on this channel. This implicitly attaches the channel if
         * not already attached.
         * @param message: the message
         * @param listener: a listener to be notified of the outcome of this message.
         * @throws AblyException
         */
        public void publish(IEnumerable<Message> messages, ICompletionListener listener)
        {
            Logger.Current.Debug("publish(Message[]); channel = " + this._name);
            if (_options != null && _options.Encrypted)
                foreach (Message message in messages)
                {
                    message.EncryptData(_cipher);
                }
            var msg = new ProtocolMessage(TAction.MESSAGE, this._name);
            msg.Messages = messages.ToList();
            switch (State)
            {
                case ChannelState.Initialised:
                case ChannelState.Attaching:
                    /* queue the message for later send */
                    _queuedMessages.Add(new QueuedMessage(msg, listener));
                    break;
                case ChannelState.Detaching:
                case ChannelState.Detached:
                case ChannelState.Failed:
                    throw new AblyException(new ErrorInfo("Unable to publish in detached or failed state", 40000, HttpStatusCode.BadRequest));
                case ChannelState.Attached:
                    ConnectionManager connectionManager = _ably.Connection.ConnectionManager;
                    connectionManager.Send(msg, _ably.Options.QueueMessages, listener);
                    break;
            }
        }

        /***
         * internal
         *
         */
        private void SendQueuedMessages()
        {
            Logger.Current.Debug("sendQueuedMessages()");
            bool queueMessages = _ably.Options.QueueMessages;
            ConnectionManager connectionManager = _ably.Connection.ConnectionManager;
            foreach (QueuedMessage msg in _queuedMessages)
                try
                {
                    connectionManager.Send(msg.Message, queueMessages, msg.Listener);
                }
                catch (AblyException e)
                {
                    Logger.Current.Error("sendQueuedMessages(): Unexpected exception sending message", e);
                    if (msg.Listener != null)
                        msg.Listener.OnError(e.ErrorInfo);
                }
        }

        private void FailQueuedMessages(ErrorInfo reason)
        {
            Logger.Current.Debug("failQueuedMessages()");
            foreach (QueuedMessage msg in _queuedMessages)
                if (msg.Listener != null)
                    try
                    {
                        msg.Listener.OnError(reason);
                    }
                    catch (Exception ex)
                    {
                        Logger.Current.Error("failQueuedMessages(): Unexpected exception calling listener", ex);
                    }
        }

        private List<QueuedMessage> _queuedMessages;

        /************************************
         * Channel history 
         ************************************/

        /**
         * Obtain recent history for this channel using the REST API.
         * The history provided relqtes to all clients of this application,
         * not just this instance.
         * @param params: the request params. See the Ably REST API
         * documentation for more details.
         * @return: an array of Messgaes for this Channel.
         * @throws AblyException
         */
        //public PaginatedResult<Message[]> history(Param[] params) throws AblyException {
        //    if(this.state == ChannelState.attached) {
        //        /* add the "attached=true" param to tell the system to look at the realtime history */
        //        Param attached = new Param("live", "true");
        //        if(params == null) params = new Param[]{ attached };
        //        else params = Param.push(params, attached);
        //    }
        //    BodyHandler<Message[]> bodyHandler = null;
        //    if(_options != null && _options.encrypted)
        //        bodyHandler = MessageReader.getMessageResponseHandler(_cipher);
        //    else
        //        bodyHandler = MessageReader.getMessageResponseHandler();
        //    return new PaginatedQuery<Message[]>(ably.http, basePath + "/history", HttpUtils.defaultGetHeaders(!ably._options.useTextProtocol), params, bodyHandler).get();
        //}

        /************************************
         * Channel _options 
         ************************************/

        public void setOptions(ChannelOptions options)
        {
            this._options = options;
            if (options != null && options.Encrypted)
                _cipher = Crypto.GetCipher(options);
            else
                _cipher = null;
        }

        /************************************
         * internal general
         * @throws AblyException 
         ************************************/

        public Channel(AblyRealtime ably, String name)
        {
            Logger.Current.Debug("RealtimeChannel(); channel = " + name);
            _ably = ably;
            Name = name;
            _basePath = "/channels/" + name;
            Presence = new Presence(this);
            State = ChannelState.Initialised;
            _queuedMessages = new List<QueuedMessage>();
        }

        public void OnChannelMessage(ProtocolMessage msg)
        {
            switch (msg.Action)
            {
                case TAction.ATTACHED:
                    SetAttached(msg);
                    break;
                case TAction.DETACHED:
                    SetDetached(msg);
                    break;
                case TAction.MESSAGE:
                    OnMessage(msg);
                    break;
                case TAction.PRESENCE:
                    OnPresence(msg);
                    break;
                case TAction.ERROR:
                    SetFailed(msg);
                    break;
                default:
                    Logger.Current.Error("onChannelMessage(): Unexpected message action (" + msg.Action + ")");
                    break;
            }
        }

        AblyRealtime _ably;
        public AblyRealtime Ably { get { return _ably; } }
        string _basePath;
        private ChannelOptions _options;
        private IChannelCipher _cipher;
    }

}
