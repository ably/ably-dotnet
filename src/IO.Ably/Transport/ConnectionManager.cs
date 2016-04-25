using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using ConnectionState = IO.Ably.Transport.States.Connection.ConnectionState;

namespace IO.Ably.Transport
{
    public class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        private readonly IAcknowledgementProcessor _ackProcessor;
        private readonly ClientOptions _options;
        private readonly Queue<ProtocolMessage> _pendingMessages;
        private readonly SynchronizationContext _sync;
        private int _connectionAttempts;
        private DateTimeOffset? _firstConnectionAttempt;
        private ConnectionState _state;

        private ITransport _transport;

        internal ConnectionManager()
        {
            _sync = SynchronizationContext.Current;
            _pendingMessages = new Queue<ProtocolMessage>();
        }

        internal ConnectionManager(ITransport transport, IAcknowledgementProcessor ackProcessor,
            ConnectionState initialState)
            : this()
        {
            _transport = transport;
            _transport.Listener = this;
            _state = initialState;
            _ackProcessor = ackProcessor;
            Connection = new Connection(this);
        }

        public ConnectionManager(ClientOptions options)
            : this()
        {
            _options = options;
            _state = new ConnectionInitializedState(this);
            _ackProcessor = new AcknowledgementProcessor();
            Connection = new Connection(this);
        }

        ConnectionState IConnectionContext.State => _state;

        ITransport IConnectionContext.Transport => _transport;

        Queue<ProtocolMessage> IConnectionContext.QueuedMessages => _pendingMessages;

        DateTimeOffset? IConnectionContext.FirstConnectionAttempt => _firstConnectionAttempt;

        int IConnectionContext.ConnectionAttempts => _connectionAttempts;

        void IConnectionContext.SetState(ConnectionState newState)
        {
            _state = newState;
            _state.OnAttachedToContext();

            _ackProcessor.OnStateChanged(newState);

            Connection.OnStateChanged(newState.State, newState.Error, newState.RetryIn ?? -1);
        }

        void IConnectionContext.CreateTransport(bool useFallbackHost)
        {
            if (_transport != null)
                (this as IConnectionContext).DestroyTransport();

            var transportParams = CreateTransportParameters(useFallbackHost);
            _transport = CreateTransport(transportParams);
            _transport.Listener = this;
        }

        void IConnectionContext.DestroyTransport()
        {
            if (_transport == null)
                return;

            _transport.Close();
            _transport.Listener = null;
            _transport = null;
        }

        void IConnectionContext.AttemptConnection()
        {
            if (_firstConnectionAttempt == null)
            {
                _firstConnectionAttempt = Config.Now();
            }
            _connectionAttempts++;
        }

        void IConnectionContext.ResetConnectionAttempts()
        {
            _firstConnectionAttempt = null;
            _connectionAttempts = 0;
        }

        public event MessageReceivedDelegate MessageReceived;

        // TODO: Find out why is this?
        public bool IsActive
        {
            get { return false; }
        }

        public Connection Connection { get; }

        public Realtime.ConnectionState ConnectionState
        {
            get { return _state.State; }
        }

        public void Connect()
        {
            _state.Connect();
        }

        public void Close()
        {
            _state.Close();
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            _ackProcessor.SendMessage(message, callback);
            _state.SendMessage(message);
        }

        public Task SendAsync(ProtocolMessage message)
        {
            var tw = new TaskWrapper();
            Send(message, tw);
            return tw;
        }

        public Task Ping()
        {
            var res = new TaskWrapper();
            ConnectionHeartbeatRequest.Execute(this, res);
            return res;
        }

        //
        // Transport communication
        //
        void ITransportListener.OnTransportConnected()
        {
            if (_sync != null)
            {
                _sync.Post(o => OnTransportConnected(), null);
                return;
            }
            OnTransportConnected();
        }

        void ITransportListener.OnTransportDisconnected()
        {
            if (_sync != null)
            {
                _sync.Post(o => OnTransportDisconnected(), null);
                return;
            }
            OnTransportDisconnected();
        }

        void ITransportListener.OnTransportError(Exception e)
        {
            if (_sync != null)
            {
                _sync.Post(o => OnTransportError((TransportState) o, e), _transport.State);
                return;
            }
            OnTransportError(_transport.State, e);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (_sync != null)
            {
                _sync.Post(o => OnTransportMessageReceived(message), null);
                return;
            }
            OnTransportMessageReceived(message);
        }

        private TransportParams CreateTransportParameters(bool useFallbackHost)
        {
            return CreateTransportParameters(_options, Connection, useFallbackHost);
        }

        internal static TransportParams CreateTransportParameters(ClientOptions options, Connection connection,
            bool useFallbackHost)
        {
            var transportParams = new TransportParams(options);
            transportParams.Host = GetHost(options, useFallbackHost);
            transportParams.Port = options.Tls ? Defaults.TlsPort : Defaults.Port;
            transportParams.FallbackHosts = Defaults.FallbackHosts;
            if (connection != null)
            {
                transportParams.ConnectionKey = connection.Key;
                transportParams.ConnectionSerial = connection.Serial.ToString();
            }
            return transportParams;
        }

        private static string GetHost(ClientOptions options, bool useFallbackHost)
        {
            var defaultHost = Defaults.RealtimeHost;
            if (useFallbackHost)
            {
                var r = new Random();
                defaultHost = Defaults.FallbackHosts[r.Next(0, 1000)%Defaults.FallbackHosts.Length];
            }
            var host = options.RealtimeHost.IsNotEmpty() ? options.RealtimeHost : defaultHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        internal virtual ITransport CreateTransport(TransportParams transportParams)
        {
            return Defaults.TransportFactories["web_socket"].CreateTransport(transportParams);
        }

        private void OnTransportConnected()
        {
            _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Connected));
        }

        private void OnTransportDisconnected()
        {
            _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));
        }

        private void OnTransportError(TransportState state, Exception e)
        {
            _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(state, e));
        }

        private void OnTransportMessageReceived(ProtocolMessage message)
        {
            Logger.Debug("ConnectionManager: Message Received {0}", message);

            var handled = _state.OnMessageReceived(message);
            handled |= _ackProcessor.OnMessageReceived(message);
            handled |= ConnectionHeartbeatRequest.CanHandleMessage(message);

            if (message.connectionSerial != null)
            {
                Connection.Serial = message.connectionSerial.Value;
            }

            MessageReceived?.Invoke(message);
        }
    }
}