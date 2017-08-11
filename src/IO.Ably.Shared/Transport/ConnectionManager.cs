using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        public Queue<MessageAndCallback> PendingMessages { get; }
        //internal readonly AsyncContextThread AsyncContextThread = new AsyncContextThread();

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
        internal event MessageReceivedDelegate MessageReceived;
        public bool IsActive => State.CanQueue && State.CanSend;
        public Connection Connection { get; }
        public ConnectionState ConnectionState => Connection.State;
        private readonly object _stateSyncLock = new object();
        private readonly object _transportQueueLock = new object();
        private readonly object _pendingQueueLock = new object();
        private volatile ConnectionStateBase _inTransitionToState;

        private List<ProtocolMessage> _queuedTransportMessages = new List<ProtocolMessage>();

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

        public ConnectionManager(Connection connection)
        {
            PendingMessages = new Queue<MessageAndCallback>();
            AttemptsInfo = new ConnectionAttemptsInfo(connection);
            Connection = connection;
            AckProcessor = new AcknowledgementProcessor(connection);


            if (Logger.IsDebug)
            {
                Execute(() => Logger.Debug("ConnectionManager thread created"));
            }
        }

        public void ClearTokenAndRecordRetry()
        {
            RestClient.AblyAuth.ExpireCurrentToken();
            AttemptsInfo.RecordTokenRetry();
        }

        public void Connect()
        {
            State.Connect();
        }

        public Task SetState(ConnectionStateBase newState, bool skipAttach = false)
        {
            if (Logger.IsDebug)
                Logger.Debug($"xx Changing state from {ConnectionState} => {newState.State}. SkipAttach = {skipAttach}.");

            _inTransitionToState = newState;

            return ExecuteOnManagerThread(async () =>
            {
                try
                {
                    lock (_stateSyncLock)
                    {
                        if (State.State == newState.State)
                        {
                            if (Logger.IsDebug)
                                Logger.Debug($"xx State is already {State.State}. Skipping SetState action.");
                            return;
                        }

                        //Abort any timers on the old state
                        State.AbortTimer();
                        if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: BeforeTransition");
                        newState.BeforeTransition();
                        if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: BeforeTransition end");

                        AttemptsInfo.UpdateAttemptState(newState);
                        Connection.UpdateState(newState);
                    }

                    if (skipAttach == false)
                    {
                        if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: Attaching state ");

                        await newState.OnAttachToContext();
                    }
                    else if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: Skipping attaching.");

                    await ProcessQueuedMessages();
                }
                catch (AblyException ex)
                {
                    Logger.Error("Error attaching to context", ex);

                    lock (_transportQueueLock)
                        _queuedTransportMessages.Clear();

                    Connection.UpdateState(newState);

                    newState.AbortTimer();

                    if (newState.State != Realtime.ConnectionState.Failed)
                    {
                        SetState(new ConnectionFailedState(this, ex.ErrorInfo));
                    }
                }
                finally
                {
                    //Clear the state in transition only if the current state hasn't updated it
                    if (_inTransitionToState == newState)
                    {
                        _inTransitionToState = null;

                    }
                }
            });
        }

        public async Task CreateTransport()
        {
            if (Logger.IsDebug) Logger.Debug("Creating transport");

            if (Transport != null)
                (this as IConnectionContext).DestroyTransport();

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
                    throw;

                throw new AblyException(ex);
            }
        }

        public void DestroyTransport(bool suppressClosedEvent)
        {
            if (Logger.IsDebug) Logger.Debug("Destroying transport");

            if (Transport == null)
                return;

            try
            {
                Transport.Listener = null;
                Transport.Dispose();
            }
            catch (Exception e)
            {
                Logger.Warning("Error while destroying transport. Nothing to worry about. Cleaning up. Error: " + e.Message);
            }
            finally
            {
                Transport = null;
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
                    await SetState(new ConnectionDisconnectedState(this), skipAttach: ConnectionState == Realtime.ConnectionState.Connecting);
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
            State.Close();
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Current state: {Connection.State}. Sending message: {message}");
            }

            var result = VerifyMessageHasCompatibleClientId(message);
            if (result.IsFailure)
            {
                callback?.Invoke(false, result.Error);
                return;
            }

            //Encode message/presence payloads
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
                    throw new AblyException($"Current state is [{State.State}] which supports queuing but Options.QueueMessages is set to False.");
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
            if (Logger.IsDebug) Logger.Debug($"Sending message ({message.Action}) to transport");
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
            ExecuteOnManagerThread(() =>
            {
                if (Logger.IsDebug)
                {
                    var errorMessage = ex != null ? $" Error: {ex.Message}" : "";
                    Logger.Debug($"Transport state changed to: {transportState}.{errorMessage}");
                }

                if (transportState == TransportState.Closed || ex != null)
                {
                    var connectionState = _inTransitionToState?.State ?? ConnectionState;
                    switch (connectionState)
                    {
                        case Realtime.ConnectionState.Closing:
                            SetState(new ConnectionClosedState(this) { Exception = ex });
                            break;
                        case Realtime.ConnectionState.Connecting:
                            HandleConnectingFailure(null, ex);
                            break;
                        case Realtime.ConnectionState.Connected:
                            var disconnectedState = new ConnectionDisconnectedState(this, GetErrorInfoFromTransportException(ex, ErrorInfo.ReasonDisconnected)) { Exception = ex };
                            disconnectedState.RetryInstantly = Connection.ConnectionResumable;
                            SetState(disconnectedState);
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

        public void HandleConnectingFailure(ErrorInfo error, Exception ex)
        {
            if (Logger.IsDebug) Logger.Debug("Handling Connecting failure.");
            ErrorInfo resolvedError = error ?? (ex != null ? new ErrorInfo(ex.Message, 80000) : null);
            if (ShouldSuspend())
            {
                SetState(new ConnectionSuspendedState(this, resolvedError ?? ErrorInfo.ReasonSuspended));
            }
            else
            {
                SetState(new ConnectionDisconnectedState(this, resolvedError ?? ErrorInfo.ReasonDisconnected));
            }
        }

        public void SendPendingMessages(bool resumed)
        {
            if (resumed)
            {
                //Resend any messages waiting an Ack Queue
                foreach (var message in AckProcessor.GetQueuedMessages())
                {
                    SendToTransport(message);
                }
            }

            lock (_pendingQueueLock)
            {
                if (Logger.IsDebug) Logger.Debug("Sending pending message: Count: " + PendingMessages.Count);

                while (PendingMessages.Count > 0)
                {
                    var queuedMessage = PendingMessages.Dequeue();
                    SendMessage(queuedMessage.Message, queuedMessage.Callback);
                }
            }
        }

        public void FailMessageWaitingForAckAndClearOutgoingQueue(RealtimeChannel channel, ErrorInfo error)
        {
            error = error ?? new ErrorInfo($"Channel cannot publish messages whilst state is {channel.State}", 50000);
            AckProcessor.FailChannelMessages(channel.Name, error);

            //TODO: Clear messages from the outgoing queue
        }

        void ITransportListener.OnTransportDataReceived(RealtimeTransportData data)
        {
            ExecuteOnManagerThread(() =>
            {
                var message = Handler.ParseRealtimeData(data);
                return OnTransportMessageReceived(message);
            });
        }

        public Task Execute(Action action)
        {
            if (Options != null && Options.UseSyncForTesting)
            {
                Task.Run(action).WaitAndUnwrapException();
                return TaskConstants.BooleanTrue;
            }

            var task = Task.Run(action);
            task.ConfigureAwait(false);
            return task;
        }

        public Task ExecuteOnManagerThread(Func<Task> asyncOperation)
        {
            if (Options.UseSyncForTesting)
            {
                asyncOperation().WaitAndUnwrapException();
                return TaskConstants.BooleanTrue;
            }

            var task = Task.Run(asyncOperation);
            task.ConfigureAwait(false);
            return task;
        }

        internal async Task<TransportParams> CreateTransportParameters()
        {
            return await TransportParams.Create(AttemptsInfo.GetHost(), RestClient.AblyAuth, Options, Connection.Key, Connection.Serial);
        }

        public async Task OnTransportMessageReceived(ProtocolMessage message)
        {
            if (_inTransitionToState != null)
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug($"InState transition to '{_inTransitionToState}'. Queuing tranport message until state updates");
                }

                lock (_transportQueueLock)
                    _queuedTransportMessages.Add(message);
            }
            else
            {
                await ProcessTransportMessage(message);
            }
        }

        private async Task ProcessQueuedMessages()
        {
            List<ProtocolMessage> copy;
            lock (_transportQueueLock)
            {
                copy = _queuedTransportMessages;
                _queuedTransportMessages = new List<ProtocolMessage>();
            }

            foreach (var message in copy)
            {
                if (Logger.IsDebug) Logger.Debug("Proccessing queued message: " + message);

                await ProcessTransportMessage(message);
            }
        }

        private async Task ProcessTransportMessage(ProtocolMessage message)
        {
            try
            {
                var handled = await State.OnMessageReceived(message);
                handled |= AckProcessor.OnMessageReceived(message);
                handled |= ConnectionHeartbeatRequest.CanHandleMessage(message);

                Connection.UpdateSerial(message);
                MessageReceived?.Invoke(message);
            }
            catch (Exception e)
            {
                Logger.Error("Error processing message: " + message, e);
                throw new AblyException(e);
            }
        }

        public void HandleNetworkStateChange(NetworkState state)
        {
            switch (state)
            {
                case NetworkState.Online:
                    if (ConnectionState == ConnectionState.Disconnected ||
                        ConnectionState == ConnectionState.Suspended)
                    {
                        if (Logger.IsDebug) Logger.Debug("Network state is Online. Attempting reconnect.");
                        Connect();
                    }
                    break;
                case NetworkState.Offline:
                    if (ConnectionState == ConnectionState.Connected ||
                        ConnectionState == ConnectionState.Connecting )
                    {
                        if (Logger.IsDebug) Logger.Debug("Network state is Offline. Moving to disconnected.");
                        SetState(new ConnectionDisconnectedState(this,
                            new ErrorInfo("Connection closed due to Operating system network going offline", 80017))
                        {
                            RetryInstantly = true
                        });
                    }
                    break;

            }
        }
    }
}