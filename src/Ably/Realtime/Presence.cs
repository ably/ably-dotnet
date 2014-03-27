using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ably.Protocol;

namespace Ably.Realtime
{
/**
 * A class that provides access to presence operations and state for the
 * associated Channel.
 */

    public class Presence
    {

        /************************************
	 * subscriptions and PresenceListener
	 ************************************/

        /**
	 * Get the presence state for this Channel.
	 * @return: the current present members.
	 * @throws AblyException 
	 */

        public IEnumerable<PresenceMessage> Get()
        {
            return presence.Values;
        }

        /**
	 * An interface allowing a listener to be notified of arrival of presence messages
	 */

        public interface IPresenceListener
        {
            void OnPresenceMessage(IEnumerable<PresenceMessage> messages);
        }

        /**
	 * Subscribe to presence events on the associated Channel. This implicitly
	 * attaches the Channel if it is not already attached.
	 * @param listener: the listener to me notified on arrival of presence messages.
	 * @throws AblyException
	 */

        public void subscribe(IPresenceListener listener)
        {
            listeners.Add(listener);
            channel.Attach();
        }

        /**
	 * Unsubscribe a previously subscribed presence listener for this channel.
	 * @param listener: the previously subscribed listener.
	 */

        public void unsubscribe(IPresenceListener listener)
        {
            listeners.Remove(listener);
        }

        /***
	 * internal
	 *
	 */

        public void SetPresence(IEnumerable<PresenceMessage> messages, bool broadcast)
        {
            foreach (PresenceMessage update in messages)
            {
                switch (update.Action)
                {
                    case PresenceMessage.ActionType.Enter:
                    case PresenceMessage.ActionType.Update:
                        presence.Add(update.ClientId, update);
                        break;
                    case PresenceMessage.ActionType.Leave:
                        presence.Remove(update.ClientId);
                        break;
                }
            }
            if (broadcast)
                broadcastPresence(messages);
        }

        private void broadcastPresence(IEnumerable<PresenceMessage> messages)
        {
            listeners.OnPresenceMessage(messages);
        }

        private Dictionary<String, PresenceMessage> presence = new Dictionary<string, PresenceMessage>();

        private PresenseMulticaster listeners = new PresenseMulticaster();

        private class PresenseMulticaster : List<IPresenceListener>, IPresenceListener
        {

            public void OnPresenceMessage(IEnumerable<PresenceMessage> messages)
            {
                foreach (var member in this)
                    try
                    {
                        member.OnPresenceMessage(messages);
                    }
                    catch (Exception t)
                    {
                    }
            }
        }

        /************************************
	 * enter/leave and pending messages
	 ************************************/

        /**
	 * Enter this client into this channel. This client will be added to the presence set
	 * and presence subscribers will see an enter message for this client.
	 * @param clientData: optional data (eg a status message) for this member.
	 * See {@link io.ably.types.Data} for the supported data types.
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void Enter(Object clientData, ICompletionListener listener)
        {
            EnterClient(clientId, clientData, listener);
        }

        /**
	 * Update the presence data for this client. If the client is not already a member of
	 * the presence set it will be added, and presence subscribers will see an enter or
	 * update message for this client.
	 * @param clientData: optional data (eg a status message) for this member.
	 * See {@link io.ably.types.Data} for the supported data types.
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void Update(Object clientData, ICompletionListener listener)
        {
            UpdateClient(clientId, clientData, listener);
        }

        /**
	 * Leave this client from this channel. This client will be removed from the presence
	 * set and presence subscribers will see a leave message for this client.
	 * @param clientData: optional data (eg a status message) for this member.
	 * See {@link io.ably.types.Data} for the supported data types.
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void Leave(ICompletionListener listener)
        {
            LeaveClient(clientId, listener);
        }

        /**
	 * Enter a specified client into this channel. The given client will be added to the
	 * presence set and presence subscribers will see a corresponding presence message.
	 * This method is provided to support connections (eg connections from application
	 * server instances) that act on behalf of multiple clientIds. In order to be able to
	 * enter the channel with this method, the client library must have been instanced
	 * either with a key, or with a token bound to the wildcard clientId.
	 * @param clientId: the id of the client.
	 * @param clientData: optional data (eg a status message) for this member.
	 * See {@link io.ably.types.Data} for the supported data types.
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void EnterClient(String clientId, Object clientData, ICompletionListener listener)
        {
            Logger.Current.Debug("enterClient(); channel = " + channel.Name + "; clientId = " + clientId);
            UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Enter, clientId, clientData), listener);
        }

        /**
	 * Update the presence data for a specified client into this channel.
	 * If the client is not already a member of the presence set it will be added, and
	 * presence subscribers will see an enter or update message for this client.
	 * As for #enterClient above, the connection must be authenticated in a way that
	 * enables it to represent an arbitrary clientId.
	 * @param clientId: the id of the client.
	 * @param clientData: optional data (eg a status message) for this member.
	 * See {@link io.ably.types.Data} for the supported data types.
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void UpdateClient(String clientId, Object clientData, ICompletionListener listener)
        {
            Logger.Current.Debug("updateClient(); channel = " + channel.Name + "; clientId = " + clientId);
            UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Update, clientId, clientData), listener);
        }

        /**
	 * Leave a given client from this channel. This client will be removed from the
	 * presence set and presence subscribers will see a leave message for this client.
	 * @param clientId: the id of the client.
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void LeaveClient(String clientId, ICompletionListener listener)
        {
            Logger.Current.Debug("leaveClient(); channel = " + channel.Name + "; clientId = " + clientId);
            UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Leave, clientId), listener);
        }

        /**
	 * Update the presence for this channel with a given PresenceMessage update.
	 * The connection must be authenticated in a way that enables it to represent
	 * the clientId in the message.
	 * @param msg: the presence message
	 * @param listener: a listener to be notified on completion of the operation.
	 * @throws AblyException
	 */

        public void UpdatePresence(PresenceMessage msg, ICompletionListener listener)
        {
            Logger.Current.Debug("update(); channel = " + channel.Name + "; clientId = " + clientId);
            if (clientId == null)
                throw new AblyException("Unable to enter presence channel without clientId", 91000,
                    HttpStatusCode.BadRequest);


            switch (channel.State)
            {
                case Channel.ChannelState.Initialised:
                    channel.Attach();
                    break;
                case Channel.ChannelState.Attaching:
                    var queued = new QueuedPresence(msg, listener);
                    _pendingPresence.Add(msg.ClientId, queued);
                    break;
                case Channel.ChannelState.Attached:
                    var message = new ProtocolMessage(TAction.PRESENCE, channel.Name);
                    message.Presence = new List<PresenceMessage> {msg};
                    var ably = channel.Ably;
                    var connectionManager = ably.Connection.ConnectionManager;
                    connectionManager.Send(message, ably.Options.QueueMessages, listener);
                    break;
                default:
                    throw new AblyException("Unable to enter presence channel in detached or failed state", 91001, HttpStatusCode.BadRequest);
            }
        }

        /************************************
	 * history
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

        //public PaginatedResult<PresenceMessage[]> history(Param[] params)
        //{
        //    if (channel.State == Channel.ChannelState.Attached)
        //    {
        //        /* add the "attached=true" param to tell the system to look at the realtime history */
        //        Param attached = new Param("live", "true");
        //        if (params ==
        //        null) params =
        //        new Param[] {attached};
        //    else
        //    params =
        //        Param.push(params,
        //        attached)
        //        ;
        //    }
        //    AblyRealtime ably = channel.ably;
        //    BodyHandler<PresenceMessage[]> bodyHandler = null;
        //    bodyHandler = PresenceReader.presenceBodyHandler;
        //    return new PaginatedQuery<PresenceMessage[]>(ably.http, channel.basePath + "/presence/history",
        //        HttpUtils.defaultGetHeaders(!ably.options.useTextProtocol),  params,
        //    bodyHandler).
        //    get();
        //}

        /***
	 * internal
	 *
	 */

        private class QueuedPresence
        {
            public PresenceMessage Msg { get; private set; }
            public ICompletionListener Listener { get; private set; }

            public QueuedPresence(PresenceMessage msg, ICompletionListener listener)
            {
                this.Msg = msg;
                this.Listener = listener;
            }
        }

        private readonly Dictionary<String, QueuedPresence> _pendingPresence = new Dictionary<string, QueuedPresence>();

        private void sendQueuedMessages()
        {
            Logger.Current.Debug("sendQueuedMessages()");
            AblyRealtime ably = channel.Ably;
            bool queueMessages = ably.Options.QueueMessages;
            ConnectionManager connectionManager = ably.Connection.ConnectionManager;
            int count = _pendingPresence.Count;
            if (count == 0)
                return;

            ProtocolMessage message = new ProtocolMessage(TAction.PRESENCE, channel.Name);
            var allQueued = _pendingPresence.Values;
            var presenceMessages = message.Presence = new List<PresenceMessage>();
            ICompletionListener listener;

            if (count == 1)
            {
                QueuedPresence queued = allQueued.First();
                presenceMessages[0] = queued.Msg;
                listener = queued.Listener;
            }
            else
            {
                int idx = 0;
                var mListener = new CompletionMulticaster();
                foreach (var queued in allQueued)
                {
                    presenceMessages[idx++] = queued.Msg;
                    if (queued.Listener != null)
                        mListener.Add(queued.Listener);
                }
                listener = mListener.Any() == false ? null : mListener;
            }
            try
            {
                connectionManager.Send(message, queueMessages, listener);
            }
            catch (AblyException e)
            {
                Logger.Current.Error("sendQueuedMessages(): Unexpected exception sending message", e);
                if (listener != null)
                    listener.OnError(e.ErrorInfo);
            }
        }

        private void failQueuedMessages(ErrorInfo reason)
        {
            Logger.Current.Debug("failQueuedMessages()");
            foreach (QueuedPresence msg in _pendingPresence.Values)
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

        /************************************
	 * attach / detach
	 ************************************/

        internal void SetAttached(IEnumerable<PresenceMessage> messages)
        {
            sendQueuedMessages();
            if (messages != null)
                SetPresence(messages, false);
        }

        internal void SetDetached(ErrorInfo reason)
        {
            failQueuedMessages(reason);
        }

        internal void SetSuspended(ErrorInfo reason)
        {
            failQueuedMessages(reason);
        }

        /************************************
	    * general
	    ************************************/

        public Presence(Channel channel)
        {
            this.channel = channel;
            clientId = channel.Ably.Options.ClientId;
        }

        private Channel channel;
        private String clientId;
    }
}
