using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ably;
using Ably.Protocol;

namespace Ably.Realtime
{
    public class DebugOptions : AblyOptions
    {
        public IRawProtocolListener ProtocolListener { get; set; }

        public DebugOptions(String key)
        {
            Key = key;
        }
    }

    public interface IRawProtocolListener
    {
        void OnRawMessage(ProtocolMessage message);
    }
    /***********************************
	 * a class encapsulating information
	 * associated with a state change
	 * request or notification
	 ***********************************/

    public class StateIndication
    {
        public ConnectionState State { get; private set; }
        public ErrorInfo Reason { get; private set; }
        public bool UseFallbackHost { get; set; }
        public StateIndication(ConnectionState state, ErrorInfo reason)
        {
            State = state;
            Reason = reason;
        }
    }

    /*************************************
     * a class encapsulating state machine
     * information for a given state
     *************************************/

    public class StateInfo
    {
        public ConnectionState State { get; private set; }
        public ErrorInfo DefaultErrorInfo { get; private set; }


        public bool QueueEvents { get; private set; }
        public bool SendEvents { get; private set; }
        public bool Terminal { get; private set; }
        public bool Retry { get; private set; }
        public long Timeout { get; private set; }

        public StateInfo(ConnectionState state, bool queueEvents, bool sendEvents, bool terminal, bool retry, long timeout, ErrorInfo defaultErrorInfo)
        {
            State = state;
            QueueEvents = queueEvents;
            SendEvents = sendEvents;
            Terminal = terminal;
            Retry = retry;
            Timeout = timeout;
            DefaultErrorInfo = defaultErrorInfo;
        }
    }

    public interface IConnectListener
    {
        void OnTransportAvailable(ITransport transport, TransportParams @params);
        void OnTransportUnavailable(ITransport transport, TransportParams @params, ErrorInfo reason);
    }

    public interface ICompletionListener
    {
        /**
     * Called when the associated operation completes successfully,
     */
        void OnSuccess();

        /**
         * Called when the associated operation completes with an error.
         * @param reason: information about the error.
         */
        void OnError(ErrorInfo reason);

        /**
         * A Multicaster instance is used in the Ably library to manage a list
         * of client listeners against certain operations.
         */

    }
    public class CompletionMulticaster : List<ICompletionListener>, ICompletionListener
    {
        public CompletionMulticaster(IEnumerable<ICompletionListener> members)
            : base(members)
        {

        }

        public CompletionMulticaster(params ICompletionListener[] members)
            : base(members)
        {

        }

        public void OnSuccess()
        {
            foreach (var member in this)
            {

                try
                {
                    member.OnSuccess();
                }
                catch (Exception t)
                {
                    Logger.Current.Error("Error executing onSuccess for " + member);
                }
            }
        }

        public void OnError(ErrorInfo reason)
        {
            foreach (var member in this)
            {

                try
                {
                    member.OnError(reason);
                }
                catch (Exception t)
                {
                    Logger.Current.Error("Error executing OnError for " + member);
                }
            }
        }
    }


    public interface ITransportFactory
    {
        ITransport GetTransport(TransportParams transportParams, ConnectionManager connectionManager);
    }

    public class QueuedMessage
    {
        public ProtocolMessage Message { get; private set; }
        public ICompletionListener Listener { get; set; }
        public bool IsMerged { get; set; }
        public QueuedMessage(ProtocolMessage message, ICompletionListener listener)
        {
            Message = message;
            Listener = listener;
        }
    }

    public class TransportParams
    {
        public AblyOptions Options { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string ConnectionId { get; set; }
        public string ConnectionSerial { get; set; }
        public TransportMode Mode { get; set; }

        public IEnumerable<KeyValuePair<string, string>> GetConnectParams(IDictionary<string, string> baseParams)
        {
            var paramList = new Dictionary<string, string>(baseParams);
            if (Options.UseTextProtocol)
                paramList.Add("binary", "false");
            if (Options.EchoMessages == false)
                paramList.Add("echo", "false");
            if (ConnectionId.IsNotEmpty())
            {
                Mode = TransportMode.Resume;
                paramList.Add("resume", ConnectionId);
                if (ConnectionSerial.IsNotEmpty())
                    paramList.Add("connection_serial", ConnectionSerial);
            }
            else if (Options.Recover != null)
            {
                Mode = TransportMode.Recover;
                Regex recoverSpec = new Regex("^(\\w+):(\\w+)$");
                var match = recoverSpec.Match(Options.Recover);
                if (match.Success)
                {
                    paramList.Add("recover", match.Groups[1].Value);
                    paramList.Add("connection_serial", match.Groups[2].Value);
                }
                else
                {
                    Logger.Current.Error("Invalid recover string specified");
                }
            }
            if (Options.ClientId.IsNotEmpty())
                paramList.Add("client_id", Options.ClientId);

            return paramList;
        }
    }

    public enum TransportMode
    {
        Clean,
        Resume,
        Recover
    }

    public interface ITransport
    {

        /**
         * Initiate a connection attempt; the transport will be activated,
         * and attempt to remain connected, until disconnect() is called.
         * @throws AblyException 
         */
        void Connect(IConnectListener connectListener);

        /**
         * Close this transport.
         */
        void Close(bool sendDisconnect);

        /**
         * Kill this transport.
         */
        void Abort(ErrorInfo reason);

        /**
         * Send a message on the channel
         * @param msg
         * @throws IOException
         */
        void Send(ProtocolMessage msg);

        String GetHost();
    }

    public class ConnectionManager : IConnectListener
    {
        private const string TAG = "ConnectionManager";
        private const string InternetCheckHost = "http://live.cdn.ably-realtime.com";
        private const string InternetCheckPath = "/is-the-internet-up.txt";
        private const string InternetCheckOk = "yes";
        private object _lock = new object();

        class PendingMessageQueue
        {
            private long startSerial = 0L;
            private object _lock = new object();
            private List<QueuedMessage> queue = new List<QueuedMessage>();
            public void Push(QueuedMessage msg)
            {
                queue.Add(msg);
            }

            public void Ack(long msgSerial, int count, ErrorInfo reason)
            {
                var ackMessages = new List<QueuedMessage>();
                var nackMessages = new List<QueuedMessage>();

                lock (_lock)
                {
                    if (msgSerial < startSerial)
                    {
                        /* this is an error condition and shouldn't happen but
                         * we can handle it gracefully by only processing the
                         * relevant portion of the response */
                        count -= (int)(startSerial - msgSerial);
                        msgSerial = startSerial;
                    }
                    if (msgSerial > startSerial)
                    {
                        /* this counts as a nack of the messages earlier than serial,
                         * as well as an ack */
                        int nCount = (int)(msgSerial - startSerial);
                        nackMessages.AddRange(queue.Take(nCount));
                        startSerial = msgSerial;
                    }
                    if (msgSerial == startSerial)
                    {
                        ackMessages.AddRange(queue.Take(count));
                        startSerial += count;
                    }
                }

                if (nackMessages.Any())
                {
                    if (reason == null)
                        reason = new ErrorInfo("Unknown error", 50000, HttpStatusCode.InternalServerError);
                    foreach (QueuedMessage msg in nackMessages)
                        if (msg.Listener != null)
                            msg.Listener.OnError(reason);
                }
                if (ackMessages.Any())
                {
                    foreach (QueuedMessage msg in ackMessages)
                        if (msg.Listener != null)
                            msg.Listener.OnSuccess();
                }
            }

            public void Nack(long serial, int count, ErrorInfo reason)
            {
                var nackMessages = new List<QueuedMessage>();
                lock (_lock)
                {
                    if (serial != startSerial)
                    {
                        /* this is an error condition and shouldn't happen but
                         * we can handle it gracefully by only processing the
                         * relevant portion of the response */
                        count -= (int)(startSerial - serial);
                        serial = startSerial;
                    }
                    nackMessages.AddRange(queue.Take(count));
                    startSerial = serial;
                }
                if (nackMessages.Any())
                {
                    if (reason == null)
                        reason = new ErrorInfo("Unknown error", 50000, HttpStatusCode.InternalServerError);
                    foreach (QueuedMessage msg in nackMessages)
                        if (msg.Listener != null)
                            msg.Listener.OnError(reason);
                }
            }
        }

        AblyRealtime _ably;
        private readonly AblyOptions _options;
        private readonly Connection _connection;
        private readonly ITransportFactory _factory;
        private readonly List<QueuedMessage> _queuedMessages;
        private readonly PendingMessageQueue _pendingMessages;

        public StateInfo State { get; private set; }
        private StateIndication _indicatedState, _requestedState;
        private ConnectParams _pendingConnect;
        private ITransport _transport;
        private long _suspendTime;
        private long _msgSerial;

        /* for choosing fallback host*/
        private static readonly Random Random = new Random();

        /* for debug/test only */
        private IRawProtocolListener _protocolListener;

        /***********************************
         * default errors
         ***********************************/

        static readonly ErrorInfo REASON_CLOSED = new ErrorInfo("Connection closed by client", 10000);
        static readonly ErrorInfo REASON_DISCONNECTED = new ErrorInfo("Connection temporarily unavailable", 80003);
        static readonly ErrorInfo REASON_SUSPENDED = new ErrorInfo("Connection unavailable", 80002);
        static readonly ErrorInfo REASON_FAILED = new ErrorInfo("Connection failed", 80000);
        static readonly ErrorInfo REASON_REFUSED = new ErrorInfo("Access refused", 40100);
        static readonly ErrorInfo REASON_TOO_BIG = new ErrorInfo("Connection closed; message too large", 40000);
        static readonly ErrorInfo REASON_NEVER_CONNECTED = new ErrorInfo("Unable to establish connection", 80002);
        static readonly ErrorInfo REASON_TIMEDOUT = new ErrorInfo("Unable to establish connection", 80014);



        /***********************
         * all state information
         ***********************/

        public static readonly Dictionary<ConnectionState, StateInfo> States = new Dictionary<ConnectionState, StateInfo>() {
    {
		ConnectionState.Initialized, new StateInfo(ConnectionState.Initialized, true, false, false, false, 0, null)},
		{ConnectionState.Connecting, new StateInfo(ConnectionState.Connecting, true, false, false, false, Defaults.ConnectTimeout, null)},
		{ConnectionState.Connected, new StateInfo(ConnectionState.Connected, false, true, false, false, 0, null)},
		{ConnectionState.Disconnected, new StateInfo(ConnectionState.Disconnected, true, false, false, true, Defaults.DisconnectTimeout, REASON_DISCONNECTED)},
		{ConnectionState.Suspended, new StateInfo(ConnectionState.Suspended, false, false, false, true, Defaults.SuspendedTimeout, REASON_SUSPENDED)},
		{ConnectionState.Closed, new StateInfo(ConnectionState.Closed, false, false, true, false, 0, REASON_CLOSED)},
		{ConnectionState.Failed, new StateInfo(ConnectionState.Failed, false, false, true, false, 0, REASON_FAILED)
	}};

        public ErrorInfo StateErrorInfo
        {
            get
            {
                return State.DefaultErrorInfo;
            }
        }

        public bool Active
        {
            get { return State.QueueEvents || State.SendEvents; }
        }

        /***********************
         * constructor
         ***********************/

        public ConnectionManager(AblyRealtime ably, Connection connection)
        {
            _ably = ably;
            _options = ably.Options;
            _connection = connection;
            _queuedMessages = new List<QueuedMessage>();
            _pendingMessages = new PendingMessageQueue();
            State = States.Get(ConnectionState.Initialized);
            /* debug options */
            if (_options is DebugOptions)
                _protocolListener = ((DebugOptions)_options).ProtocolListener;

            SetSuspendTime();
        }

        /*********************
         * host management
         *********************/

        public String GetHost()
        {
            String result = null;
            if (_transport != null)
                result = _transport.GetHost();
            return result;
        }

        /*********************
         * state management
         *********************/

        public StateInfo GetConnectionState()
        {
            return State;
        }

        private void SetState(StateIndication newState)
        {
            Logger.Current.Debug("SetState(): setting " + newState.State);
            ConnectionStateChange change;
            StateInfo newStateInfo = States.Get(newState.State);
            lock (_lock)
            {
                ErrorInfo reason = newState.Reason ?? newStateInfo.DefaultErrorInfo;
                change = new ConnectionStateChange(State.State, newState.State, newStateInfo.Timeout, reason);
                State = newStateInfo;
            }
            /* broadcast state change */
            _connection.OnConnectionStateChanged(change);

            /* if now connected, send queued messages, etc */
            if (State.SendEvents)
                SendQueuedMessages();
            else if (!State.QueueEvents)
            {
                FailQueuedMessages(State.DefaultErrorInfo);
                foreach (var channel in _ably.Channels.Values)
                    channel.SetSuspended(State.DefaultErrorInfo);
            }
        }

        public void RequestState(ConnectionState state)
        {
            RequestState(new StateIndication(state, null));
        }

        public void RequestState(StateIndication state)
        {
            Logger.Current.Debug("requestState(): requesting " + state.State + "; id = " + _connection.Id);
            _requestedState = state;
        }

        private void NotifyState(ITransport transport, StateIndication state)
        {
            if (_transport == transport)
            {
                /* if this transition signifies the end of the transport, clear the transport */
                if (States.Get(state.State).Terminal)
                    transport = null;
                NotifyState(state);
            }
        }

        private void NotifyState(StateIndication state)
        {
            Logger.Current.Debug("notifyState(): notifying " + state.State + "; id = " + _connection.Id);
            _indicatedState = state;
        }

        /***************************************
         * transport events/notifications
         ***************************************/

        void OnMessage(ProtocolMessage message)
        {
            if (_protocolListener != null)
                _protocolListener.OnRawMessage(message);
            switch (message.Action)
            {
                case TAction.HEARTBEAT:
                    break;
                case TAction.ERROR:
                    ErrorInfo reason = message.Error;
                    if (reason == null)
                    {
                        Logger.Current.Error("onMessage(): ERROR message received (no error detail)");
                        return;
                    }
                    Logger.Current.Error("onMessage(): ERROR message received; message = " + reason.Reason + "; code = " + reason.Code);
                    /* an error message may signify an error state in a channel, or in the connection */
                    if (message.Channel != null)
                        OnChannelMessage(message);
                    else
                        OnError(message);
                    break;
                case TAction.CONNECTED:
                    OnConnected(message);
                    break;
                case TAction.DISCONNECTED:
                    OnDisconnected(message);
                    break;
                case TAction.ACK:
                    OnAck(message);
                    break;
                case TAction.NACK:
                    OnNack(message);
                    break;
                default:
                    OnChannelMessage(message);
                    break;
            }
        }

        private void OnChannelMessage(ProtocolMessage message)
        {
            _connection.Serial = message.ConnectionSerial;
            _ably.Channels.OnChannelMessage(_transport, message);
        }

        private void OnConnected(ProtocolMessage message)
        {
            _connection.Id = message.ConnectionId;
            _msgSerial = 0;
            SetSuspendTime();
            NotifyState(new StateIndication(ConnectionState.Connected, null));
        }

        private void OnDisconnected(ProtocolMessage message)
        {
            _connection.Id = null;
            NotifyState(new StateIndication(ConnectionState.Disconnected, null));
        }

        private void OnError(ProtocolMessage message)
        {
            _connection.Id = null;
            NotifyState(new StateIndication(ConnectionState.Failed, message.Error));
        }

        private void OnAck(ProtocolMessage message)
        {
            _pendingMessages.Ack(message.MsgSerial, message.Count, message.Error);
        }

        private void OnNack(ProtocolMessage message)
        {
            _pendingMessages.Nack(message.MsgSerial, message.Count, message.Error);
        }

        /**************************
         * ConnectionManager thread
         **************************/

        private void HandleStateRequest()
        {
            bool handled = false;
            switch (_requestedState.State)
            {
                case ConnectionState.Failed:
                    if (_transport != null)
                    {
                        _transport.Abort(_requestedState.Reason);
                        handled = true;
                    }
                    break;
                case ConnectionState.Closed:
                    if (_transport != null)
                    {
                        _transport.Close(State.State == ConnectionState.Connected);
                        handled = true;
                    }
                    break;
                case ConnectionState.Connecting:
                    ConnectImpl(_requestedState);
                    handled = true;
                    break;
            }
            if (!handled)
            {
                /* the transport wasn't there, so we just transition directly */
                _indicatedState = _requestedState;
            }
            _requestedState = null;
        }

        private void HandleStateChange(StateIndication stateChange)
        {
            /* if we have had a disconnected state indication
             * from the transport then we have to decide whether
             * to transition to disconnected to suspended depending
             * on when we last had a successful connection */
            if (State.State == ConnectionState.Connecting && stateChange.State == ConnectionState.Disconnected)
            {
                stateChange = CheckSuspend(stateChange);
                _pendingConnect = null;
            }
            if (stateChange != null)
                SetState(stateChange);
        }

        private void SetSuspendTime()
        {
            _suspendTime = (DateService.NowInUnixMilliseconds + Defaults.SuspendedTimeout);
        }

        private StateIndication CheckSuspend(StateIndication stateChange)
        {
            /* We got here when a connection attempt failed and we need to check to
             * see whether we should go into disconnected or suspended state.
             * There are three options:
             * - First check to see whether or not internet connectivity is ok;
             *   if so we'll trigger a new connect attempt with a fallback host.
             * - we're entering disconnected and will schedule a retry after the
             *   reconnect timer;
             * - the suspend timer has expired, so we're going into suspended state.
             */

            /* FIXME: we might want to limit this behaviour to only a specific
             * set of error codes */
            if (_pendingConnect != null && !_pendingConnect.Fallback && CheckConnectivity())
            {
                String[] fallbackHosts = Defaults.GetFallbackHosts(_options);
                if (fallbackHosts != null && fallbackHosts.Any())
                {
                    /* we will try a fallback host */
                    StateIndication fallbackConnectRequest = new StateIndication(ConnectionState.Connecting, null);
                    fallbackConnectRequest.UseFallbackHost = true;
                    RequestState(fallbackConnectRequest);
                    /* returning null ensures we stay in the connecting state */
                    return null;
                }
            }
            var suspendMode = DateService.NowInUnixMilliseconds > _suspendTime;
            ConnectionState expiredState = suspendMode ? ConnectionState.Suspended : ConnectionState.Disconnected;
            return new StateIndication(expiredState, null);
        }

        private void TryWait(long timeout)
        {
            if (_requestedState == null && _indicatedState == null)
            {

            }
        }

        public void Run()
        {
            StateIndication stateChange;
            while (true)
            {
                stateChange = null;
                lock (_lock)
                {
                    /* if we're initialising, then tell the starting thread that
                     * we're ready to receive events */
                    //if (State.State == ConnectionState.Initialized)
                    //    Notify();

                    while (stateChange == null)
                    {
                        TryWait(State.Timeout);
                        /* if during the wait some action was requested, handle it */
                        if (_requestedState != null)
                        {
                            HandleStateRequest();
                            continue;
                        }

                        /* if during the wait we were told that a transition
                         * needs to be enacted, handle that (outside the lock) */
                        if (_indicatedState != null)
                        {
                            stateChange = _indicatedState;
                            _indicatedState = null;
                            break;
                        }

                        /* if our state wants us to retry on timer expiry, do that */
                        if (State.Retry)
                        {
                            RequestState(ConnectionState.Connecting);
                            continue;
                        }

                        /* no indicated state or requested action, so the timer
                         * expired while we were in the connecting state */
                        stateChange = CheckSuspend(new StateIndication(ConnectionState.Disconnected, REASON_TIMEDOUT));
                    }
                }
                if (stateChange != null)
                    HandleStateChange(stateChange);
            }
        }

        public void OnTransportAvailable(ITransport transport, TransportParams @params)
        {
            _transport = transport;
        }

        public void OnTransportUnavailable(ITransport transport, TransportParams @params, ErrorInfo reason)
        {
            NotifyState(new StateIndication(ConnectionState.Disconnected, reason));
        }

        private class ConnectParams : TransportParams
        {
            public bool Fallback { get; private set; }

            public ConnectParams(AblyOptions options, bool fallback, Connection connection)
            {
                Fallback = fallback;
                Options = options;
                ConnectionId = connection.Id;
                ConnectionSerial = connection.Serial.ToString();
                if (fallback && (Defaults.GetFallbackHosts(options)).Any())
                {
                    var fallbackHosts = Defaults.GetFallbackHosts(options);
                    Host = fallbackHosts[Random.Next(fallbackHosts.Length)];
                }
                else
                {
                    Host = Defaults.GetHost(options);
                }
                Port = Defaults.GetPort(options);
            }
        }

        private void ConnectImpl(StateIndication request)
        {
            /* determine the parameters of this connection attempt, and
             * instance the transport.
             * First, choose the transport. (Right now there's only one.)
             * Second, choose the host. ConnectParams will use the default
             * (or requested) host, unless fallback=true, in which case
             * it will choose a fallback host at random */
            _pendingConnect = new ConnectParams(_options, request.UseFallbackHost, _connection);

            /* enter the connecting state */
            NotifyState(request);

            /* try the connection */
            ITransport transport;
            try
            {
                transport = Config.GetTransport(_pendingConnect, this);
            }
            catch (Exception e)
            {
                String msg = "Unable to instance transport class";
                Logger.Current.Error(msg, e);
                throw new Exception(msg, e);
            }
            transport.Connect(this);
        }

        /**
         * Determine whether or not the client has connection to the network
         * without reference to a specific ably host. This is to determine whether
         * it is better to try a fallback host, or keep retrying with the default
         * host.
         * @return boolean, true if network is available
         */
        protected bool CheckConnectivity()
        {
            try
            {
                var client = new AblyHttpClient(InternetCheckHost, null, false);
                var request = new AblyRequest(InternetCheckPath, HttpMethod.Get);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Content-Type", "application/json");
                var response = client.Execute(request);
                return InternetCheckOk.Equals(response.TextResponse);
            }
            catch (AblyException e)
            {
                return false;
            }
        }

        /******************
         * event queueing
         ******************/



        public void Send(ProtocolMessage msg, bool queueEvents, ICompletionListener listener)
        {
            StateInfo state;

            state = State;
            if (state.SendEvents)
            {
                SendImpl(msg, listener);
                return;
            }
            if (state.QueueEvents && queueEvents)
            {
                int queueSize = _queuedMessages.Count;
                if (queueSize > 0)
                {
                    QueuedMessage lastQueued = _queuedMessages.Last();
                    ProtocolMessage lastMessage = lastQueued.Message;
                    if (ProtocolMessage.mergeTo(lastMessage, msg))
                    {
                        if (!lastQueued.IsMerged)
                        {
                            lastQueued.Listener = new CompletionMulticaster(lastQueued.Listener);
                            lastQueued.IsMerged = true;
                        }
                        ((CompletionMulticaster)lastQueued.Listener).Add(listener);
                        return;
                    }
                }
                _queuedMessages.Add(new QueuedMessage(msg, listener));
                return;
            }
            throw new AblyException(state.DefaultErrorInfo);
        }

        private void SendImpl(ProtocolMessage message)
        {
            _transport.Send(message);
        }

        private void SendImpl(ProtocolMessage message, ICompletionListener listener)
        {

            if (ProtocolMessage.AckRequired(message))
            {
                message.MsgSerial = _msgSerial++;
                _pendingMessages.Push(new QueuedMessage(message, listener));
            }
            _transport.Send(message);
        }

        private void SendImpl(QueuedMessage msg)
        {
            ProtocolMessage message = msg.Message;
            if (ProtocolMessage.AckRequired(message))
            {
                message.MsgSerial = _msgSerial++;
                _pendingMessages.Push(msg);
            }
            _transport.Send(message);
        }



        private void SendQueuedMessages()
        {
            lock (_lock)
            {
                while (_queuedMessages.Any())
                {
                    try
                    {
                        var message = _queuedMessages.First();
                        SendImpl(message);
                        _queuedMessages.Remove(message);
                    }
                    catch (AblyException e)
                    {
                        Logger.Current.Error("sendQueuedMessages(): Unexpected error sending queued messages", e);
                    }
                }
            }
        }

        private void FailQueuedMessages(ErrorInfo reason)
        {
            lock (_lock)
            {
                foreach (var message in _queuedMessages)
                {
                    try
                    {
                        if (message.Listener != null)
                            message.Listener.OnError(reason);
                    }
                    catch (Exception ex)
                    {
                        Logger.Current.Error("failQueuedMessages(): Unexpected error calling listener", ex);
                    }
                }
            }
        }
    }
}
