﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Queue<MessageAndCallback> _pendingMessages;
        internal readonly AsyncContextThread AsyncContextThread = new AsyncContextThread();
        private ITransportFactory GetTransportFactory() => Options.TransportFactory ?? Defaults.WebSocketTransportFactory;
        public IAcknowledgementProcessor AckProcessor { get; internal set; }
        internal ConnectionAttemptsInfo AttemptsInfo { get; }
        public TimeSpan RetryTimeout => Options.DisconnectedRetryTimeout;
        public AblyRest RestClient => Connection.RestClient;
        public MessageHandler Handler => RestClient.MessageHandler;
        public ConnectionState State => Connection.ConnectionState;
        public TransportState TransportState => Transport.State;
        public ITransport Transport { get; private set; }
        public ClientOptions Options => RestClient.Options;
        public TimeSpan DefaultTimeout => Options.RealtimeRequestTimeout;
        public TimeSpan SuspendRetryTimeout => Options.SuspendedRetryTimeout;
        public event MessageReceivedDelegate MessageReceived;
        public bool IsActive => State.CanQueue && State.CanSend;
        public Connection Connection { get; }
        public ConnectionStateType ConnectionState => Connection.State;
        private readonly object _stateSyncLock = new object();
        private volatile ConnectionState _inTransitionToState;

        public void ClearAckQueueAndFailMessages(ErrorInfo error) => AckProcessor.ClearQueueAndFailMessages(error);
        public Task<bool> CanUseFallBackUrl(ErrorInfo error)
        {
            return AttemptsInfo.CanFallback(error);
        }

        public ConnectionManager(Connection connection)
        {
            _pendingMessages = new Queue<MessageAndCallback>();
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
            RestClient.Auth.ExpireCurrentToken();
            AttemptsInfo.RecordTokenRetry();
        }

        public void Connect()
        {
            State.Connect();
        }

        public Task SetState(ConnectionState newState, bool skipAttach = false)
        {
            if (Logger.IsDebug) Logger.Debug($"xx Changing state from {ConnectionState} => {newState.State}. SkipAttach = {skipAttach}.");

            _inTransitionToState = newState;

            return ExecuteOnManagerThread(async () =>
            {
                try
                {

                    lock (_stateSyncLock)
                    {
                        if (State.State == newState.State)
                        {
                            if(Logger.IsDebug) Logger.Debug($"xx State is already {State.State}. Skipping SetState action.");
                            return;
                        }

                        //Abort any timers on the old state
                        State.AbortTimer();
                        if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: BeforeTransition");
                        newState.BeforeTransition();

                        AttemptsInfo.UpdateAttemptState(newState);
                        Connection.UpdateState(newState);
                    }

                    if (skipAttach == false)
                    {
                        if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: Attaching state ");

                        await newState.OnAttachToContext();
                    }
                    else
                        if (Logger.IsDebug) Logger.Debug($"xx {newState.State}: Skipping attaching.");
                }
                catch (AblyException ex)
                {
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
                    //Clear the state in transition only if the current state hasn't updated it
                    if (_inTransitionToState == newState)
                    {
                        _inTransitionToState = null;
                    }
                    if (Logger.IsDebug)
                    {
                        Logger.Debug($"xx {newState.State}: Completed setting state");
                    }
                }
            });
        }

        public async Task CreateTransport()
        {
            if (Logger.IsDebug) Logger.Debug("Creating transport");
            
            if (Transport != null)
                (this as IConnectionContext).DestroyTransport();

            var transport = GetTransportFactory().CreateTransport(await CreateTransportParameters());
            transport.Listener = this;
            Transport = transport;
            Transport.Connect();
        }

        public void DestroyTransport(bool suppressClosedEvent)
        {
            if (Logger.IsDebug) Logger.Debug("Destroying transport");

            if (Transport == null)
                return;

            Transport.Close(suppressClosedEvent);
            Transport.Listener = null;
            Transport = null;
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
                    await SetState(new ConnectionDisconnectedState(this), skipAttach: ConnectionState == ConnectionStateType.Connecting);
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

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Current state: {Connection.State}. Sending message: {message}");
            }

            if (State.CanSend)
            {
                SendMessage(message, callback);
                return;
            }

            if (State.CanQueue && Options.QueueMessages)
            {
                lock (_pendingMessages)
                {
                    _pendingMessages.Enqueue(new MessageAndCallback(message, callback));
                }
                return;
            }

            if (State.CanQueue && Options.QueueMessages == false)
            {
                throw new AblyException($"Current state is [{State.State}] which supports queuing but Options.QueueMessages is set to False.");
            }
            throw new AblyException($"The current state [{State.State}] does not allow messages to be sent.");

        }

        private void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            AckProcessor.QueueIfNecessary(message, callback);

            SendToTransport(message);
        }

        public void SendToTransport(ProtocolMessage message)
        {
            var data = Handler.GetTransportData(message);
            Transport.Send(data);
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
                        case ConnectionStateType.Closing:
                            SetState(new ConnectionClosedState(this) {Exception = ex});
                            break;
                        case ConnectionStateType.Connecting:
                            HandleConnectingFailure(null, ex);
                            break;
                        case ConnectionStateType.Connected:
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

            lock (_pendingMessages)
            {
                while (_pendingMessages.Count > 0)
                {
                    var queuedMessage = _pendingMessages.Dequeue();
                    SendMessage(queuedMessage.Message, queuedMessage.Callback);
                } 
            }
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
            return await TransportParams.Create(AttemptsInfo.GetHost(), RestClient.Auth, Options, Connection.Key, Connection.Serial);
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