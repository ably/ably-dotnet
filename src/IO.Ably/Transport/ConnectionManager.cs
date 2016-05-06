using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using ConnectionState = IO.Ably.Transport.States.Connection.ConnectionState;

namespace IO.Ably.Transport
{
    internal class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        public IAcknowledgementProcessor AckProcessor { get; internal set; }
        public ClientOptions Options { get; }
        public TimeSpan DefaultTimeout => Options.RealtimeRequestTimeout;

        private readonly Queue<ProtocolMessage> _pendingMessages;
        private int _connectionAttempts;
        private DateTimeOffset? _firstConnectionAttempt;
        private ConnectionState _state;

        internal readonly AsyncContextThread AsyncContextThread = new AsyncContextThread();

        public Task ExecuteAsyncOperation(Func<Task> asyncOperation)
        {
            if (Options.UseSyncForTesting)
            {
                asyncOperation().WaitAndUnwrapException();
                return TaskConstants.BooleanTrue;
            }
            return AsyncContextThread.Factory.Run(asyncOperation);
        }

        public Task<T> ExecuteAsyncOperation<T>(Func<Task<T>>  asyncOperation)
        {
            return AsyncContextThread.Factory.Run(asyncOperation);
        }

        public AblyRest RestClient { get; private set; }

        private ITransport _transport;

        private ConnectionManager()
        {
            _pendingMessages = new Queue<ProtocolMessage>();
            _state = new ConnectionInitializedState(this);
            Connection = new Connection(this);
        }

        internal ConnectionManager(ITransport transport, IAcknowledgementProcessor ackProcessor,
            ConnectionState initialState, AblyRest restClient)
            : this()
        {
            _transport = transport;
            _transport.Listener = this;
            _state = initialState;
            RestClient = restClient;
            AckProcessor = ackProcessor;
            Connection = new Connection(this);
        }

        public ConnectionManager(ClientOptions options, AblyRest restClient)
            : this()
        {
            Options = options;
            RestClient = restClient;
            AckProcessor = new AcknowledgementProcessor();
        }

        ConnectionState IConnectionContext.State => _state;

        public ITransport Transport => _transport;

        Queue<ProtocolMessage> IConnectionContext.QueuedMessages => _pendingMessages;

        DateTimeOffset? IConnectionContext.FirstConnectionAttempt => _firstConnectionAttempt;

        int IConnectionContext.ConnectionAttempts => _connectionAttempts;

        void IConnectionContext.SetState(ConnectionState newState)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Setting state from {_state} to {newState}");
            }

            ExecuteAsyncOperation(async () =>
            {
                _state = newState;
                AckProcessor.OnStateChanged(newState);

                Connection.OnStateChanged(newState.State, newState.Error, newState.RetryIn);

                try
                {
                    await _state.OnAttachedToContext();
                }
                catch (AblyException ex)
                {
                    Logger.Error("Error attaching to context", ex);
                    if (_state.State != ConnectionStateType.Failed)
                    {
                        ((IConnectionContext) this).SetState(new ConnectionFailedState(this,
                            new ErrorInfo($"Failed to attach connection state {_state.State}", 500)));
                    }
                }
            });
        }

        async Task IConnectionContext.CreateTransport(bool renewToken = false)
        {
            if (_transport != null)
                (this as IConnectionContext).DestroyTransport();

            if(renewToken)
                RestClient.AblyAuth.ExpireCurrentToken(); //This will cause the RequestToken to be called when constructing the TransportParams

            var transport = GetTransportFactory().CreateTransport(await CreateTransportParameters());
            transport.Listener = this;
            _transport = transport;
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

        public async Task<bool> CanConnectToAbly()
        {
            if (Options.SkipInternetCheck)
                return await TaskConstants.BooleanTrue;

            try
            {
                var httpClient = RestClient.HttpClient;
                var request = new AblyRequest(Defaults.InternetCheckURL, HttpMethod.Get);
                var response = await httpClient.Execute(request);
                return response.TextResponse == Defaults.InternetCheckOKMessage;
            }
            catch (Exception ex)
            {
                Logger.Error("Error accessing ably internet check url. Internet is down!", ex);
                return false;
            }
        }

        public void SetConnectionClientId(string clientId)
        {
            if(clientId.IsNotEmpty())
                RestClient.AblyAuth.ConnectionClientId = clientId;
        }

        public bool ShouldWeRenewToken(ErrorInfo error)
        {
            if (error == null) return false;

            return error.IsTokenError && RestClient.AblyAuth.TokenRenewable;
        }

        public event MessageReceivedDelegate MessageReceived;

        // TODO: Find out why is this?
        public bool IsActive
        {
            get { return false; }
        }

        public Connection Connection { get; internal set; }

        public ConnectionStateType ConnectionState => _state.State;

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
            if (Logger.IsDebug)
            {
                Logger.Debug($"Current state: {_state}. Sending message: {message}");
            }
            AckProcessor.SendMessage(message, callback);
            _state.SendMessage(message);
        }

        public Task SendAsync(ProtocolMessage message)
        {
            var tw = new TaskWrapper();
            Send(message, tw.Callback);
            return tw.Task;
        }

        public Task<Result<TimeSpan?>> PingAsync()
        {
            return TaskWrapper.Wrap<TimeSpan?>(Ping);
        }
        public void Ping(Action<TimeSpan?, ErrorInfo> callback)
        {
            ConnectionHeartbeatRequest.Execute(this, callback);
        }

        //
        // Transport communication
        //
        void ITransportListener.OnTransportConnected()
        {
            OnTransportConnected();
        }

        void ITransportListener.OnTransportDisconnected()
        {
            
            OnTransportDisconnected();
        }

        void ITransportListener.OnTransportError(Exception e)
        {
            
            OnTransportError(_transport.State, e);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Message received: " + message);
            }
            ExecuteAsyncOperation(() => OnTransportMessageReceived(message));
        }

        internal async Task<TransportParams> CreateTransportParameters()
        {
            return await TransportParams.Create(RestClient.Auth, Options, Connection?.Key, Connection?.Serial);
        }

        //TODO: Move this inside WebSocketTransport
        private static string GetHost(ClientOptions options, bool useFallbackHost)
        {
            var defaultHost = Defaults.RealtimeHost;
            if (useFallbackHost)
            {
                var r = new Random();
                defaultHost = Defaults.FallbackHosts[r.Next(0, 1000) % Defaults.FallbackHosts.Length];
            }
            var host = options.RealtimeHost.IsNotEmpty() ? options.RealtimeHost : defaultHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        private ITransportFactory GetTransportFactory()
        {
            return Options.TransportFactory ?? Defaults.WebSocketTransportFactory;
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

        private async Task OnTransportMessageReceived(ProtocolMessage message)
        {
            Logger.Debug("ConnectionManager: Message Received {0}", message);

            var handled = await _state.OnMessageReceived(message);
            handled |= AckProcessor.OnMessageReceived(message);
            handled |= ConnectionHeartbeatRequest.CanHandleMessage(message);

            if (message.connectionSerial != null)
            {
                Connection.Serial = message.connectionSerial.Value;
            }

            MessageReceived?.Invoke(message);
        }
    }
}