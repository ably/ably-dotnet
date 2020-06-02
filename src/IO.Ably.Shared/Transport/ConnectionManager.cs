﻿using System;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using IO.Ably.Utils;

namespace IO.Ably.Transport
{
    internal class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        internal Func<DateTimeOffset> Now { get; set; }

        internal ILogger Logger { get; }

        private ITransportFactory GetTransportFactory()
            => Options.TransportFactory ?? Defaults.WebSocketTransportFactory;

        public TimeSpan RetryTimeout => Options.DisconnectedRetryTimeout;

        public AblyRest RestClient => Connection.RestClient;

        public MessageHandler Handler => RestClient.MessageHandler;

        public ConnectionStateBase State => Connection.ConnectionState;

        public ITransport Transport { get; internal set; }

        public ClientOptions Options => RestClient.Options;

        public TimeSpan DefaultTimeout => Options.RealtimeRequestTimeout;

        public TimeSpan SuspendRetryTimeout => Options.SuspendedRetryTimeout;

        public Connection Connection { get; }

        public ConnectionState ConnectionState => Connection.State;

        public ConnectionManager(Connection connection, Func<DateTimeOffset> nowFunc, ILogger logger)
        {
            Now = nowFunc;
            Logger = logger ?? DefaultLogger.LoggerInstance;
            Connection = connection;
        }

        public RealtimeCommand Connect()
        {
            return State.Connect();
        }

        public async Task CreateTransport(string host)
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
                var transport = GetTransportFactory().CreateTransport(await CreateTransportParameters(host));
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

                throw new AblyException(new ErrorInfo("Error creating Socket Transport", ErrorCodes.ConnectionFailed, HttpStatusCode.ServiceUnavailable), ex);
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

        public bool ShouldWeRenewToken(ErrorInfo error, RealtimeState state)
        {
            if (error == null)
            {
                return false;
            }

            return error.IsTokenError && state.AttemptsInfo.TriedToRenewToken == false && RestClient.AblyAuth.TokenRenewable;
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
                    Auth = new AuthDetails
                    {
                        AccessToken = tokenDetails.Token,
                    },
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
                        {
                            throw new AblyException(
                                new ErrorInfo(
                                $"Connection state didn't change after Auth updated within {Defaults.DefaultRealtimeTimeout}",
                                40140));
                        }

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
                    throw new AblyException(
                        new ErrorInfo($"Error while waiting for connection to update after updating Auth"), e);
                }
            }
        }

        public void Send(
            ProtocolMessage message,
            Action<bool, ErrorInfo> callback = null,
            ChannelOptions channelOptions = null)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Current state: {Connection.State}. Sending message: {message}");
            }

            if (message.ConnectionId.IsNotEmpty())
            {
                Logger.Warning(
                    "Setting ConnectionId to null. ConnectionId should never be included in an outbound message on a realtime connection, it’s always implicit");
                message.ConnectionId = null;
            }

            var result = VerifyMessageHasCompatibleClientId(message);
            if (result.IsFailure)
            {
                callback?.Invoke(false, result.Error);
                return;
            }

            // TODO: What happens if a message fails encoding before being sent.
            var encodingResult = Handler.EncodeProtocolMessage(message, channelOptions.ToEncodingDecodingContext());

            if (State.CanSend == false && State.CanQueue == false)
            {
                throw new AblyException($"The current state [{State.State}] does not allow messages to be sent.");
            }

            if (State.CanSend == false && State.CanQueue && Options.QueueMessages == false)
            {
                throw new AblyException(
                    $"Current state is [{State.State}] which supports queuing but Options.QueueMessages is set to False.",
                    Connection.ConnectionState.DefaultErrorInfo.Code,
                    HttpStatusCode.ServiceUnavailable);
            }

            ExecuteCommand(SendMessageCommand.Create(message, callback).TriggeredBy("ConnectionManager.Send()"));

            Result VerifyMessageHasCompatibleClientId(ProtocolMessage protocolMessage)
            {
                var messagesResult = RestClient.AblyAuth.ValidateClientIds(protocolMessage.Messages);
                var presenceResult = RestClient.AblyAuth.ValidateClientIds(protocolMessage.Presence);

                return Result.Combine(messagesResult, presenceResult);
            }
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
                ((ITransportListener)this).OnTransportEvent(Transport.Id, TransportState.Closed, e);
            }
        }

        void ITransportListener.OnTransportEvent(Guid transportId, TransportState transportState, Exception ex)
            => ExecuteCommand(HandleTrasportEventCommand.Create(transportId, transportState, ex).TriggeredBy("ConnectionManager.OnTransportEvent()"));

        void ITransportListener.OnTransportDataReceived(RealtimeTransportData data)
        {
            var message = Handler.ParseRealtimeData(data);
            ExecuteCommand(ProcessMessageCommand.Create(message).TriggeredBy("ConnectionManager.OnTransportDataReceived()"));
        }

        public void ExecuteCommand(RealtimeCommand cmd)
        {
            Connection.RealtimeClient.Workflow.QueueCommand(cmd);
        }

        internal async Task<TransportParams> CreateTransportParameters(string host)
        {
            return await TransportParams.Create(
                host,
                RestClient.AblyAuth,
                Options,
                Connection.Key,
                Connection.Serial);
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

                        ExecuteCommand(ConnectCommand.Create().TriggeredBy("ConnectionManager.HandleNetworkStateChange(Online)"));
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
                            new ErrorInfo(
                                "Connection disconnected due to Operating system network going offline",
                                80017);
                        ExecuteCommand(SetDisconnectedStateCommand.Create(errorInfo, retryInstantly: true).TriggeredBy("ConnectionManager.HandleNetworkStateChange(Offline)"));
                    }

                    break;
            }
        }
    }
}
