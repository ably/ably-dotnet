using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using IO.Ably.MessageEncoders;
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
        private ITransportFactory GetTransportFactory() => Options.TransportFactory ?? Defaults.WebSocketTransportFactory;
        public IAcknowledgementProcessor AckProcessor { get; internal set; }
        private ConnectionAttemptsInfo AttemptsInfo { get; }
        public TimeSpan RetryTimeout => Options.DisconnectedRetryTimeout;
        public AblyRest RestClient { get; }
        public MessageHandler Handler => RestClient.MessageHandler;
        public ConnectionState State => Connection.ConnectionState;
        public TransportState TransportState => Transport.State;
        public ITransport Transport { get; private set; }
        Queue<ProtocolMessage> IConnectionContext.QueuedMessages => _pendingMessages;
        public ClientOptions Options => RestClient.Options;
        public TimeSpan DefaultTimeout => Options.RealtimeRequestTimeout;
        public TimeSpan SuspendRetryTimeout => Options.SuspendedRetryTimeout;
        public event MessageReceivedDelegate MessageReceived;
        public bool IsActive => State.CanQueue && State.CanSend;
        public Connection Connection { get; }
        public ConnectionStateType ConnectionState => Connection.State;


        public ConnectionManager(Connection connection, AblyRest restClient)
        {
            _pendingMessages = new Queue<ProtocolMessage>();
            AttemptsInfo = new ConnectionAttemptsInfo(restClient.Options, connection);
            Connection = connection;
            RestClient = restClient;
            AckProcessor = new AcknowledgementProcessor();

            if (Logger.IsDebug)
            {
                Execute(() => Logger.Debug("ConnectionManager thread created"));
            }
        }
        
        public void ClearTokenAndRecordRetry()
        {
            RestClient.Auth.ExpireCurrentToken();
            AttemptsInfo.RecordTokenRetry();
        }

        public void Connect()
        {
            Execute(() => State.Connect());
        }

        public Task SetState(ConnectionState newState, bool skipAttach = false)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Setting state from {ConnectionState} to {newState}");
            }

            return ExecuteOnManagerThread(async () =>
            {
                //Abort any times on the old state
                State.AbortTimer();

                AckProcessor.OnStateChanged(newState);
                bool statusUpdated = false;
                if (skipAttach == false)
                {
                    try
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug($"xx Attaching state " + newState.State);
                        }
                        
                        await newState.OnAttachedToContext();
                    }
                    catch (AblyException ex)
                    {
                        statusUpdated = true;
                        Connection.UpdateState(newState);

                        newState.AbortTimer();

                        Logger.Error("Error attaching to context", ex);
                        if (newState.State != ConnectionStateType.Failed)
                        {
                            SetState(new ConnectionFailedState(this, ex.ErrorInfo));
                        }
                    }
                    finally
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug($"xx Completed attaching " + newState.State);
                        }
                    }
                }

                if (statusUpdated == false)
                    Connection.UpdateState(newState);

            });
        }

        async Task IConnectionContext.CreateTransport()
        {
            if (Logger.IsDebug) Logger.Debug("Creating transport");
            AttemptsInfo.Increment();

            if (Transport != null && AttemptsInfo.TriedToRenewToken)
                (this as IConnectionContext).DestroyTransport();

            var transport = GetTransportFactory().CreateTransport(await CreateTransportParameters());
            transport.Listener = this;
            Transport = transport;
            Transport.Connect();
        }

        void IConnectionContext.DestroyTransport(bool suppressClosedEvent)
        {
            if (Logger.IsDebug) Logger.Debug("Destroying transport");

            if (Transport == null)
                return;

            Transport.Close(suppressClosedEvent);
            Transport.Listener = null;
            Transport = null;
        }

        void IConnectionContext.ResetConnectionAttempts()
        {
            AttemptsInfo.Reset();
        }

        public async Task<bool> CanConnectToAbly()
        {
            if (Options.SkipInternetCheck)
                return true;

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

            return error.IsTokenError && AttemptsInfo.TriedToRenewToken == false && RestClient.AblyAuth.TokenRenewable;
        }

        public bool ShouldSuspend()
        {
            return AttemptsInfo.ShouldSuspend();
        }

        public async Task<bool> RetryBecauseOfTokenError(ErrorInfo error)
        {
            if (error != null && error.IsTokenError)
            {
                if (ShouldWeRenewToken(error))
                {
                    ClearTokenAndRecordRetry();
                    await SetState(new ConnectionDisconnectedState(this), skipAttach: true);
                    await SetState(new ConnectionConnectingState(this));
                }
                else
                {
                    SetState(new ConnectionFailedState(this, error));
                }

                return true;
            }
            return false;
        }

        public void CloseConnection()
        {
            Execute(() => State.Close());
        }


        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Current state: {Connection.State}. Sending message: {message}");
            }

            AckProcessor.SendMessage(message, callback);

            var data = Handler.GetTransportData(message);

            Transport.Send(data);
        }

        void ITransportListener.OnTransportEvent(TransportState state, Exception ex)
        {
            ExecuteOnManagerThread(() =>
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug($"On transport event. State {state} error {ex}");
                }

                if (state == TransportState.Closed || ex != null)
                {
                    switch (ConnectionState)
                    {
                        case ConnectionStateType.Closing:
                            SetState(new ConnectionClosedState(this));
                            break;
                        case ConnectionStateType.Connecting:
                            HandleConnectingFailure(ex);
                            break;
                        case ConnectionStateType.Connected:
                            var error = ex != null ? ErrorInfo.ReasonDisconnected : null;
                            SetState(new ConnectionDisconnectedState(this, GetErrorInfoFromTransportException(ex, ErrorInfo.ReasonDisconnected)));
                            break;
                    }
                }

                return TaskConstants.BooleanTrue;
            });
        }

        private static ErrorInfo GetErrorInfoFromTransportException(Exception ex, ErrorInfo @default)
        {
            if (ex?.Message == "HTTP/1.1 401 Unauthorized")
                return ErrorInfo.ReasonRefused;

            return @default;
        }

        public void HandleConnectingFailure(Exception ex)
        {
            if (Logger.IsDebug) Logger.Debug("Handling Connecting failure.");
            if (ShouldSuspend())
                SetState(new ConnectionSuspendedState(this));
            else
                SetState(new ConnectionDisconnectedState(this, ErrorInfo.ReasonDisconnected));
        }

        void ITransportListener.OnTransportDataReceived(RealtimeTransportData data)
        {
            ExecuteOnManagerThread(() =>
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Message received: " + data.Explain());
                }

                var message = Handler.ParseRealtimeData(data);
                return OnTransportMessageReceived(message);
            });
        }

        public Task Execute(Action action)
        {
            if (Options != null && Options.UseSyncForTesting)
            {
                AsyncContextThread.Factory.StartNew(action).WaitAndUnwrapException();
                return TaskConstants.BooleanTrue;
            }

            return AsyncContextThread.Factory.StartNew(action);
        }

        public Task ExecuteOnManagerThread(Func<Task> asyncOperation)
        {
            if (Options.UseSyncForTesting)
            {
                asyncOperation().WaitAndUnwrapException();
                return TaskConstants.BooleanTrue;
            }

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
                defaultHost = Defaults.FallbackHosts[r.Next(0, 1000) % Defaults.FallbackHosts.Length];
            }
            var host = options.RealtimeHost.IsNotEmpty() ? options.RealtimeHost : defaultHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        

        public async Task OnTransportMessageReceived(ProtocolMessage message)
        {
            Logger.Debug("ConnectionManager: Message Received {0}", message);

            var handled = await State.OnMessageReceived(message);
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