using Ably.Types;
using System;
using System.Collections.Generic;
using Ably.Realtime;

namespace Ably.Transport
{
    public class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        internal ConnectionManager()
        {
            this.sync = System.Threading.SynchronizationContext.Current;
            this.pendingMessages = new Queue<ProtocolMessage>();
        }

        internal ConnectionManager(ITransport transport, IAcknowledgementProcessor ackProcessor, States.Connection.ConnectionState initialState)
            : this()
        {
            this.transport = transport;
            this.transport.Listener = this;
            this.state = initialState;
            this.ackProcessor = ackProcessor;
            this.connection = new Connection(this);
        }

        public ConnectionManager(AblyRealtimeOptions options)
            : this()
        {
            this.options = options;
            this.state = new States.Connection.ConnectionInitializedState(this);
            this.ackProcessor = new AcknowledgementProcessor();
            this.connection = new Connection(this);
        }

        private ITransport transport;
        private System.Threading.SynchronizationContext sync;
        private ILogger Logger = Config.AblyLogger;
        private Queue<ProtocolMessage> pendingMessages;
        private AblyRealtimeOptions options;
        private States.Connection.ConnectionState state;
        private IAcknowledgementProcessor ackProcessor;
        private DateTimeOffset? _firstConnectionAttempt;
        private int _connectionAttempts;
        private Connection connection;

        public event MessageReceivedDelegate MessageReceived;

        // TODO: Find out why is this?
        public bool IsActive
        {
            get { return false; }
        }

        States.Connection.ConnectionState IConnectionContext.State
        {
            get { return state; }
        }

        ITransport IConnectionContext.Transport
        {
            get
            {
                return this.transport;
            }
        }

        Queue<ProtocolMessage> IConnectionContext.QueuedMessages
        {
            get
            {
                return this.pendingMessages;
            }
        }

        DateTimeOffset? IConnectionContext.FirstConnectionAttempt
        {
            get
            {
                return _firstConnectionAttempt;
            }
        }

        int IConnectionContext.ConnectionAttempts
        {
            get
            {
                return _connectionAttempts;
            }
        }

        public Connection Connection
        {
            get
            {
                return connection;
            }
        }

        public ConnectionState ConnectionState
        {
            get
            {
                return this.state.State;
            }
        }

        public void Connect()
        {
            this.state.Connect();
        }

        public void Close()
        {
            this.state.Close();
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            ackProcessor.SendMessage(message, callback);
            state.SendMessage(message);
        }

        public void Ping(Action<bool, ErrorInfo> callback)
        {
            ConnectionHeartbeatRequest.Execute(this, callback);
        }

        internal static TransportParams CreateTransportParameters(AblyRealtimeOptions options)
        {
            TransportParams transportParams = new TransportParams(options);
            transportParams.Host = GetHost(options);
            transportParams.Port = options.Tls ? Defaults.TlsPort : Transport.Defaults.Port;
            transportParams.FallbackHosts = Defaults.FallbackHosts;
            return transportParams;
        }

        private static string GetHost(AblyRealtimeOptions options)
        {
            string host = !string.IsNullOrEmpty(options.Host) ? options.Host : Defaults.RealtimeHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        //
        // Transport communication
        //
        void ITransportListener.OnTransportConnected()
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportConnected()), null);
                return;
            }
            this.OnTransportConnected();
        }

        void ITransportListener.OnTransportDisconnected()
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportDisconnected()), null);
                return;
            }
            this.OnTransportDisconnected();
        }

        void ITransportListener.OnTransportError(Exception e)
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportError((TransportState)o, e)), transport.State);
                return;
            }
            this.OnTransportError(transport.State, e);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportMessageReceived(message)), null);
                return;
            }
            this.OnTransportMessageReceived(message);
        }

        private void OnTransportConnected()
        {
            this.state.OnTransportStateChanged(new States.Connection.ConnectionState.TransportStateInfo(TransportState.Connected));
        }

        private void OnTransportDisconnected()
        {
            this.state.OnTransportStateChanged(new States.Connection.ConnectionState.TransportStateInfo(TransportState.Closed));
        }

        private void OnTransportError(TransportState state, Exception e)
        {
            this.state.OnTransportStateChanged(new States.Connection.ConnectionState.TransportStateInfo(state, e));
        }

        private void OnTransportMessageReceived(ProtocolMessage message)
        {
            // If the state didn't handle the message, handle it here
            // TODO: Chenge with can handle instead of did handle
            bool handled = this.state.OnMessageReceived(message);
            handled |= this.ackProcessor.OnMessageReceived(message);
            handled |= ConnectionHeartbeatRequest.CanHandleMessage(message); 

            if (message.ConnectionSerial != null)
            {
                this.connection.Serial = message.ConnectionSerial.Value;
            }

            if (!handled)
            {
                ProcessProtocolMessage(message);
            }
        }

        void IConnectionContext.SetState(States.Connection.ConnectionState newState)
        {
            this.state = newState;
            this.state.OnAttachedToContext();

            this.ackProcessor.OnStateChanged(newState);

            this.connection.OnStateChanged(newState.State, newState.Error);
        }

        void IConnectionContext.CreateTransport()
        {
            if (this.transport != null)
                (this as IConnectionContext).DestroyTransport();

            TransportParams transportParams = CreateTransportParameters(options);
            this.transport = Defaults.TransportFactories["web_socket"].CreateTransport(transportParams);
            this.transport.Listener = this;
        }

        void IConnectionContext.DestroyTransport()
        {
            if (this.transport == null)
                return;

            this.transport.Close();
            this.transport.Listener = null;
            this.transport = null;
        }

        void IConnectionContext.AttemptConnection()
        {
            if (_firstConnectionAttempt == null)
            {
                _firstConnectionAttempt = DateTimeOffset.Now;
            }
            _connectionAttempts++;
        }

        void IConnectionContext.ResetConnectionAttempts()
        {
            _firstConnectionAttempt = null;
            _connectionAttempts = 0;
        }

        private void ProcessProtocolMessage(ProtocolMessage message)
        {
            this.Logger.Verbose("ConnectionManager: Message Received {0}", message);

            if (this.MessageReceived != null)
            {
                this.MessageReceived(message);
            }
        }
    }
}
