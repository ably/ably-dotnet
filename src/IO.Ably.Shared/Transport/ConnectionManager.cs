using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using IO.Ably.Utils;
using Microsoft.Win32;

namespace IO.Ably.Transport
{
    internal class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        internal Func<DateTimeOffset> Now { get; set; }

        internal ILogger Logger { get; }

        public Queue<MessageAndCallback> PendingMessages { get; }

        private ITransportFactory GetTransportFactory()
            => Options.TransportFactory ?? Defaults.WebSocketTransportFactory;

        public IAcknowledgementProcessor AckProcessor { get; internal set; }

        internal ConnectionAttemptsInfo AttemptsInfo { get; }

        public TimeSpan RetryTimeout => Options.DisconnectedRetryTimeout;

        public AblyRest RestClient => Connection.RestClient;

        public MessageHandler Handler => RestClient.MessageHandler;

        public ConnectionStateBase State => Connection.ConnectionState;

        public ITransport Transport { get; private set; }

        public ClientOptions Options => RestClient.Options;

        public TimeSpan DefaultTimeout => Options.RealtimeRequestTimeout;

        public TimeSpan SuspendRetryTimeout => Options.SuspendedRetryTimeout;

        public Connection Connection { get; }

        public ConnectionState ConnectionState => Connection.State;

        private readonly object _stateSyncLock = new object();
        private readonly object _pendingQueueLock = new object();
        private volatile ConnectionStateBase _inTransitionToState;

        private ConcurrentQueue<ProtocolMessage> _queuedTransportMessages = new ConcurrentQueue<ProtocolMessage>();

        public void ClearAckQueueAndFailMessages(ErrorInfo error) => AckProcessor.ClearQueueAndFailMessages(error);

        public Task<bool> CanUseFallBackUrl(ErrorInfo error)
        {
            return AttemptsInfo.CanFallback(error);
        }

        public void DetachAttachedChannels(ErrorInfo error)
        {
            Logger.Warning("Force detaching all attached channels because the connection did not resume successfully!");

            foreach (var channel in Connection.RealtimeClient.Channels)
            {
                if (channel.State == ChannelState.Attached || channel.State == ChannelState.Attaching)
                {
                    (channel as RealtimeChannel).SetChannelState(ChannelState.Detached, error);
                }
            }
        }

        public ConnectionManager(Connection connection, Func<DateTimeOffset> nowFunc, ILogger logger)
        {
            Now = nowFunc;
            Logger = logger ?? DefaultLogger.LoggerInstance;
            PendingMessages = new Queue<MessageAndCallback>();
            AttemptsInfo = new ConnectionAttemptsInfo(connection, nowFunc);
            Connection = connection;
            AckProcessor = new AcknowledgementProcessor(connection);
        }

        public void ClearTokenAndRecordRetry()
        {
            RestClient.AblyAuth.ExpireCurrentToken();
            AttemptsInfo.RecordTokenRetry();
        }

        public RealtimeCommand Connect()
        {
            return State.Connect();
        }

        public async Task SetState(ConnectionStateBase newState, bool skipAttach = false)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug(
                    $"xx Changing state from {ConnectionState} => {newState.State}. SkipAttach = {skipAttach}.");
            }

            _inTransitionToState = newState;

            try
            {
                if (!newState.IsUpdate)
                {
                    if (State.State == newState.State)
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug($"xx State is already {State.State}. Skipping SetState action.");
                        }

                        return;
                    }

                    AttemptsInfo.UpdateAttemptState(newState);
                    State.AbortTimer();
                }

                if (Logger.IsDebug)
                {
                    Logger.Debug($"xx {newState.State}: BeforeTransition");
                }

                newState.BeforeTransition();

                if (Logger.IsDebug)
                {
                    Logger.Debug($"xx {newState.State}: BeforeTransition end");
                }

                if (skipAttach == false)
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"xx {newState.State}: Attaching state ");
                    }

                    await newState.OnAttachToContext();
                }
                else if (Logger.IsDebug)
                {
                    Logger.Debug($"xx {newState.State}: Skipping attaching.");
                }

                Connection.UpdateState(newState);

                ProcessQueuedMessages();
            }
            catch (AblyException ex)
            {
                Logger.Error("Error attaching to context", ex);

                _queuedTransportMessages = new ConcurrentQueue<ProtocolMessage>();

                Connection.UpdateState(newState);

                newState.AbortTimer();

                // RSA4c2 & RSA4d
                if (newState.State == ConnectionState.Connecting &&
                    ex.ErrorInfo.Code == 80019 & !ex.ErrorInfo.IsForbiddenError)
                {
                    ExecuteCommand(SetDisconnectedStateCommand.Create(ex.ErrorInfo));
                }
                else if (newState.State != ConnectionState.Failed)
                {
                    ExecuteCommand(SetFailedStateCommand.Create(ex.ErrorInfo));
                }
            }
            finally
            {
                // Clear the state in transition only if the current state hasn't updated it
                if (_inTransitionToState == newState)
                {
                    _inTransitionToState = null;
                }
            }
        }

        public async Task CreateTransport()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Creating transport");
            }

            if (Transport != null)
            {
                DestroyTransport(false);
            }

            try
            {
                var transport = GetTransportFactory().CreateTransport(await CreateTransportParameters());
                transport.Listener = this;
                Transport = transport;
                Transport.Connect();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while creating transport!.", ex.Message);
                if (ex is AblyException)
                {
                    throw;
                }

                throw new AblyException(ex);
            }
        }

        public void DestroyTransport(bool suppressClosedEvent)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Destroying transport");
            }

            if (Transport == null)
            {
                return;
            }

            try
            {
                Transport.Listener = null;
                Transport.Dispose();
            }
            catch (Exception e)
            {
                Logger.Warning("Error while destroying transport. Nothing to worry about. Cleaning up. Error: " +
                               e.Message);
            }
            finally
            {
                Transport = null;
            }
        }

        public bool ShouldWeRenewToken(ErrorInfo error)
        {
            if (error == null)
            {
                return false;
            }

            return error.IsTokenError && AttemptsInfo.TriedToRenewToken == false && RestClient.AblyAuth.TokenRenewable;
        }

        public bool ShouldSuspend()
        {
            return AttemptsInfo.ShouldSuspend();
        }

        public async Task<RealtimeCommand> RetryAuthentication(ErrorInfo error = null, bool updateState = true)
        {
            ClearTokenAndRecordRetry();

            if (updateState)
            {
                return ListCommand.Create(
                        SetDisconnectedStateCommand.Create(error, skipAttach: ConnectionState == ConnectionState.Connecting),
                        SetConnectingStateCommand.Create()
                        );
            }
            else
            {
                await RestClient.AblyAuth.AuthorizeAsync();
                return EmptyCommand.Instance;
            }
        }

        public void CloseConnection()
        {
            State.Close();
        }

        internal async Task OnAuthUpdated(TokenDetails tokenDetails, bool wait)
        {
            if (Connection.State != ConnectionState.Connected)
            {
                ExecuteCommand(Connect());
                return;
            }

            try
            {
                var msg = new ProtocolMessage(ProtocolMessage.MessageAction.Auth)
                {
                    Auth = new AuthDetails { AccessToken = tokenDetails.Token }
                };

                Send(msg);
            }
            catch (AblyException e)
            {
                Logger.Warning("OnAuthUpdated: closing transport after send failure");
                Logger.Debug(e.Message);
                Transport?.Close();
            }


            if (wait)
            {
                var waiter = new ConnectionChangeAwaiter(Connection);

                try
                {
                    while (true)
                    {
                        var (success, newState) = await waiter.Wait(Defaults.DefaultRealtimeTimeout);
                        if (success == false)
                            throw new AblyException(new ErrorInfo(
                                $"Connection state didn't change after Auth updated within {Defaults.DefaultRealtimeTimeout}", 40140));

                        switch (newState)
                        {
                            case ConnectionState.Connected:
                                Logger.Debug("onAuthUpdated: Successfully connected");
                                return;
                            case ConnectionState.Connecting:
                            case ConnectionState.Disconnected:
                                continue;
                            default: // if it's one of the failed states
                                Logger.Debug("onAuthUpdated: Failed to reconnect");
                                throw new AblyException(Connection.ErrorReason);
                        }
                    }

                }
                catch (AblyException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.Error("Error in AuthUpdated handler", e);
                    throw new AblyException(new ErrorInfo($"Error while waiting for connection to update after updating Auth"), e);
                }
            }
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Current state: {Connection.State}. Sending message: {message}");
            }

            if (message.ConnectionId.IsNotEmpty())
            {
                Logger.Warning("Setting ConnectionId to null. ConnectionId should never be included in an outbound message on a realtime connection, it’s always implicit");
                message.ConnectionId = null;
            }

            var result = VerifyMessageHasCompatibleClientId(message);
            if (result.IsFailure)
            {
                callback?.Invoke(false, result.Error);
                return;
            }

            // Encode message/presence payloads
            Handler.EncodeProtocolMessage(message, channelOptions);

            if (State.CanSend)
            {
                SendMessage(message, callback);
                return;
            }

            if (State.CanQueue)
            {
                if (Options.QueueMessages)
                {
                    lock (_pendingQueueLock)
                    {
                        if (Logger.IsDebug) { Logger.Debug($"Queuing message with action: {message.Action}. Connection State: {ConnectionState}"); }
                        PendingMessages.Enqueue(new MessageAndCallback(message, callback));
                    }
                }
                else
                {
                    throw new AblyException(
                        $"Current state is [{State.State}] which supports queuing but Options.QueueMessages is set to False.",
                        Connection.ConnectionState.DefaultErrorInfo.Code,
                        HttpStatusCode.ServiceUnavailable);
                }

                return;
            }

            throw new AblyException($"The current state [{State.State}] does not allow messages to be sent.");
        }

        private Result VerifyMessageHasCompatibleClientId(ProtocolMessage protocolMessage)
        {
            var messagesResult = RestClient.AblyAuth.ValidateClientIds(protocolMessage.Messages);
            var presenceResult = RestClient.AblyAuth.ValidateClientIds(protocolMessage.Presence);

            return Result.Combine(messagesResult, presenceResult);
        }

        private void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            AckProcessor.QueueIfNecessary(message, callback);
            SendToTransport(message);
        }

        public void SendToTransport(ProtocolMessage message)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Sending message ({message.Action}) to transport");
            }

            var data = Handler.GetTransportData(message);
            try
            {
                Transport.Send(data);
            }
            catch (Exception e)
            {
                Logger.Error("Error while sending to transport. Trying to reconnect.", e);
                ((ITransportListener)this).OnTransportEvent(TransportState.Closed, e);
            }
        }

        void ITransportListener.OnTransportEvent(TransportState transportState, Exception ex)
        {
            if (Logger.IsDebug)
            {
                var errorMessage = ex != null ? $" Error: {ex.Message}" : string.Empty;
                Logger.Debug($"Transport state changed to: {transportState}.{errorMessage}");
            }

            if (transportState == TransportState.Closed || ex != null)
            {
                var connectionState = _inTransitionToState?.State ?? ConnectionState;
                switch (connectionState)
                {
                    case ConnectionState.Closing:
                        ExecuteCommand(SetClosedStateCommand.Create(exception: ex));
                        break;
                    case ConnectionState.Connecting:
                        HandleConnectingFailure(null, ex);
                        break;
                    case ConnectionState.Connected:
                        var errorInfo = GetErrorInfoFromTransportException(ex, ErrorInfo.ReasonDisconnected);
                        var disconnectCommand = SetDisconnectedStateCommand.Create(errorInfo, retryInstantly: Connection.ConnectionResumable, exception: ex);
                        ExecuteCommand(disconnectCommand);
                        break;
                }
            }
        }

        private static ErrorInfo GetErrorInfoFromTransportException(Exception ex, ErrorInfo @default)
        {
            if (ex?.Message == "HTTP/1.1 401 Unauthorized")
            {
                return ErrorInfo.ReasonRefused;
            }

            return @default;
        }

        public void HandleConnectingFailure(ErrorInfo error, Exception ex)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Handling Connecting failure.");
            }

            ErrorInfo resolvedError = error ?? (ex != null ? new ErrorInfo(ex.Message, 80000) : null);
            if (ShouldSuspend())
            {
                ExecuteCommand(SetSuspendedStateCommand.Create(resolvedError ?? ErrorInfo.ReasonSuspended));
            }
            else
            {
                ExecuteCommand(SetDisconnectedStateCommand.Create(resolvedError ?? ErrorInfo.ReasonDisconnected));
            }
        }

        public void SendPendingMessages(bool resumed)
        {
            if (resumed)
            {
                // Resend any messages waiting an Ack Queue
                foreach (var message in AckProcessor.GetQueuedMessages())
                {
                    SendToTransport(message);
                }
            }

            lock (_pendingQueueLock)
            {
                if (Logger.IsDebug && PendingMessages.Count > 0)
                {
                    Logger.Debug("Sending pending message: Count: " + PendingMessages.Count);
                }

                while (PendingMessages.Count > 0)
                {
                    var queuedMessage = PendingMessages.Dequeue();
                    SendMessage(queuedMessage.Message, queuedMessage.Callback);
                }
            }
        }

        void ITransportListener.OnTransportDataReceived(RealtimeTransportData data)
        {
            var message = Handler.ParseRealtimeData(data);
            Connection.SetConfirmedAlive();
            OnTransportMessageReceived(message);
        }

        public void ExecuteCommand(RealtimeCommand cmd)
        {
            Connection.RealtimeClient.Workflow.QueueCommand(cmd);
        }

        internal async Task<TransportParams> CreateTransportParameters()
        {
            return await TransportParams.Create(AttemptsInfo.GetHost(), RestClient.AblyAuth, Options, Connection.Key, Connection.Serial);
        }

        public void OnTransportMessageReceived(ProtocolMessage message)
        {
            _queuedTransportMessages.Enqueue(message);

            if (_inTransitionToState == null)
            {
                ProcessQueuedMessages();
            }
        }

        private void ProcessQueuedMessages()
        {
            while (_queuedTransportMessages != null && _queuedTransportMessages.TryDequeue(out var message))
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Proccessing queued message: " + message);
                }

                ProcessTransportMessage(message);
            }
        }

        private void ProcessTransportMessage(ProtocolMessage message)
        {
            ExecuteCommand(ProcessMessageCommand.Create(message));
        }

        public void HandleNetworkStateChange(NetworkState state)
        {
            switch (state)
            {
                case NetworkState.Online:
                    if (ConnectionState == ConnectionState.Disconnected ||
                        ConnectionState == ConnectionState.Suspended)
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug("Network state is Online. Attempting reconnect.");
                        }

                        ExecuteCommand(ConnectCommand.Create());
                    }

                    break;
                case NetworkState.Offline:
                    if (ConnectionState == ConnectionState.Connected ||
                        ConnectionState == ConnectionState.Connecting)
                    {
                        if (Logger.IsDebug)
                        {
                            Logger.Debug("Network state is Offline. Moving to disconnected.");
                        }

                        // RTN20a
                        var errorInfo =
                            new ErrorInfo("Connection disconnected due to Operating system network going offline",
                                80017);
                        ExecuteCommand(SetDisconnectedStateCommand.Create(errorInfo, retryInstantly: true));
                    }

                    break;
            }
        }


    }
}
