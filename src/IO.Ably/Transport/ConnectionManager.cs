using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;

namespace IO.Ably.Transport
{
    internal class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        private readonly Queue<ProtocolMessage> _pendingMessages;

        internal readonly AsyncContextThread AsyncContextThread = new AsyncContextThread();
        private ConnectionState _state;

        private ConnectionManager()
        {
            _pendingMessages = new Queue<ProtocolMessage>();
            _state = new ConnectionInitializedState(this);
            Connection = new Connection(this);

            if (Logger.IsDebug)
            {
                Execute(() => Logger.Debug("ConnectionManager thread created"));
            }
        }

        internal ConnectionManager(ITransport transport, IAcknowledgementProcessor ackProcessor,
            ConnectionState initialState, AblyRest restClient)
            : this()
        {
            Transport = transport;
            Transport.Listener = this;
            _state = initialState;
            RestClient = restClient;
            AckProcessor = ackProcessor;
            Connection = new Connection(this);
        }

        public ConnectionManager(AblyRest restClient)
            : this()
        {
            AttemptsInfo = new ConnectionAttemptsInfo(restClient.Options, Connection);
            RestClient = restClient;
            AckProcessor = new AcknowledgementProcessor();
        }

        public IAcknowledgementProcessor AckProcessor { get; internal set; }

        private ConnectionAttemptsInfo AttemptsInfo { get; }
        public TimeSpan RetryTimeout => Options.DisconnectedRetryTimeout;

        public AblyRest RestClient { get; }

        ConnectionState IConnectionContext.State => _state;

        public ITransport Transport { get; private set; }

        Queue<ProtocolMessage> IConnectionContext.QueuedMessages => _pendingMessages;

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
            if (Transport != null)
                (this as IConnectionContext).DestroyTransport();

            if (renewToken)
                RestClient.AblyAuth.ExpireCurrentToken();
                    //This will cause the RequestToken to be called when constructing the TransportParams

            var transport = GetTransportFactory().CreateTransport(await CreateTransportParameters());
            transport.Listener = this;
            Transport = transport;
        }

        void IConnectionContext.DestroyTransport()
        {
            if (Transport == null)
                return;

            Transport.Close();
            Transport.Listener = null;
            Transport = null;
        }

        void IConnectionContext.AttemptConnection()
        {
            AttemptsInfo.Increment();
        }

        void IConnectionContext.ResetConnectionAttempts()
        {
            AttemptsInfo.Reset();
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
            if (clientId.IsNotEmpty())
                RestClient.AblyAuth.ConnectionClientId = clientId;
        }

        public bool ShouldWeRenewToken(ErrorInfo error)
        {
            if (error == null) return false;

            return error.IsTokenError && RestClient.AblyAuth.TokenRenewable;
        }

        public bool ShouldSuspend()
        {
            return AttemptsInfo.ShouldSuspend();
        }

        public ClientOptions Options => RestClient.Options;
        public TimeSpan DefaultTimeout => Options.RealtimeRequestTimeout;

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
            OnTransportError(Transport.State, e);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Message received: " + message);
            }
            ExecuteAsyncOperation(() => OnTransportMessageReceived(message));
        }

        public void Execute(Action action)
        {
            AsyncContextThread.Factory.StartNew(action).WaitAndUnwrapException();
        }

        public Task ExecuteAsyncOperation(Func<Task> asyncOperation)
        {
            if (Options.UseSyncForTesting)
            {
                asyncOperation().WaitAndUnwrapException();
                return TaskConstants.BooleanTrue;
            }

            return AsyncContextThread.Factory.Run(asyncOperation);
        }

        public Task<T> ExecuteAsyncOperation<T>(Func<Task<T>> asyncOperation)
        {
            return AsyncContextThread.Factory.Run(asyncOperation);
        }

        internal async Task<TransportParams> CreateTransportParameters()
        {
            return await TransportParams.Create(RestClient.Auth, Options, Connection?.Key, Connection?.Serial);
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