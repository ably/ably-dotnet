using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Realtime.Workflow
{
    internal class RealtimeState
    {
        public class ConnectionState
        {
            public Guid ConnectionId { get; } = Guid.NewGuid(); // Used to identify the connection for Os Event subscribers
            public DateTimeOffset? ConfirmedAliveAt { get; set; }

            /// <summary>
            ///     The id of the current connection. This string may be
            ///     used when recovering connection state.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            ///     The serial number of the last message received on this connection.
            ///     The serial number may be used when recovering connection state.
            /// </summary>
            public long? Serial { get; set; }

            internal long MessageSerial { get; set; } = 0;

            /// <summary>
            /// </summary>
            public string Key { get; set; }

            public TimeSpan ConnectionStateTtl { get; internal set; } = Defaults.ConnectionStateTtl;

            /// <summary>
            ///     Information relating to the transition to the current state,
            ///     as an Ably ErrorInfo object. This contains an error code and
            ///     message and, in the failed state in particular, provides diagnostic
            ///     error information.
            /// </summary>
            public ErrorInfo ErrorReason { get; set; }
        }

        public List<PingRequest> PingRequests { get; set; } = new List<PingRequest>();
    }


    internal class RealtimeWorkflow : IQueueCommand
    {
        public static class Timers
        {
            public const string PingTimer = "PingTimer";
        }

        public Connection Connection { get; }
        public RealtimeChannels Channels { get; }

        public ConnectionManager ConnectionManager => Connection.ConnectionManager;
        public ILogger Logger { get; }

        private RealtimeState State { get; }
        private Func<DateTimeOffset> Now => Connection.Now;

        internal ConnectionHeartbeatHandler HeartbeatHandler { get; }

        internal ChannelMessageProcessor ChannelMessageProcessor { get; }

        internal List<(string, Func<ProtocolMessage, RealtimeState, ValueTask<bool>>)> MessageHandlers;

        internal readonly Channel<RealtimeCommand> RealtimeMessageLoop = Channel.CreateUnbounded<RealtimeCommand>(
            new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false
            });

        public RealtimeWorkflow(Connection connection, RealtimeChannels channels, ILogger logger)
        {
            Connection = connection;
            Channels = channels;
            Logger = logger;

            HeartbeatHandler = new ConnectionHeartbeatHandler(Connection.ConnectionManager, logger);
            ChannelMessageProcessor = new ChannelMessageProcessor(connection.RealtimeClient.Channels, logger);
            MessageHandlers = new List<(string, Func<ProtocolMessage, RealtimeState, ValueTask<bool>>)>
            {
                ("State handler",(message, state) => ConnectionManager.State.OnMessageReceived(message, state)),
                ("Heartbeat handler", HeartbeatHandler.OnMessageReceived),
                ("Ack handler", (message, state) => ConnectionManager.AckProcessor.OnMessageReceived(message, state))
            };

            State = new RealtimeState();
        }

        public void Start()
        {
            Task.Run(Consume);
        }

        public void QueueCommand(params RealtimeCommand[] commands)
        {
            foreach (var command in commands)
            {
                var writeResult = RealtimeMessageLoop.Writer.TryWrite(command);
                if (writeResult == false) //Should never happen as we don't close the channels
                {
                    Logger.Warning(
                        $"Cannot schedule command: {command.Explain()} because the execution channel is closed");
                }
            }
        }

        private async Task Consume()
        {
            var reader = RealtimeMessageLoop.Reader;
            while (await reader.WaitToReadAsync())
            {
                if (reader.TryRead(out RealtimeCommand cmd))
                {
                    try
                    {
                        int level = 0;
                        var cmds = new List<RealtimeCommand> {cmd};
                        while (cmds.Count > 0)
                        {
                            if (level > 5)
                            {
                                throw new Exception("Something is wrong. There shouldn't be 5 levels of nesting");
                            }

                            var cmdsToExecute = cmds.ToArray();
                            cmds.Clear();

                            foreach (var cmdToExecute in cmdsToExecute)
                            {
                                try
                                {
                                    var result = await ProcessCommand(cmdToExecute);
                                    cmds.AddRange(result);
                                }
                                catch (Exception e)
                                {
                                    Logger.Error("Error Processing command: " + cmdsToExecute.ToString(), e);
                                }
                            }

                            level ++;
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO: Figure out there will be a case where the library becomes in an invalid state
                        // TODO: Emit the error on the connection object
                        Logger.Error("Error processing command: " + cmd.Explain(), e);
                    }
                }
            }
        }

        private void DelayCommandHandler(TimeSpan delay, RealtimeCommand cmd) =>
            Task.Delay(delay).ContinueWith(_ => QueueCommand(cmd));


        /// <summary>
        /// Processes a command and return a list of commands that need to be immediately executed
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="AblyException"></exception>
        private async Task<IEnumerable<RealtimeCommand>> ProcessCommand(RealtimeCommand command)
        {
            try
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Executing command: " + command.Explain());
                }

                switch (command)
                {
                    case ConnectCommand _:
                        var nextCommand = ConnectionManager.Connect();
                        var initFailedChannelsOnConnect =
                            ChannelCommand.CreateForAllChannels(InitialiseFailedChannelsOnConnect.Create());
                        return new [] { initFailedChannelsOnConnect, nextCommand };
                    case CloseConnectionCommand _:
                        ConnectionManager.CloseConnection();
                        break;
                    case RetryAuthCommand retryAuth:
                        await ConnectionManager.RetryAuthentication(updateState: retryAuth.UpdateState);
                        break;
                    case SetConnectedStateCommand cmd:
                        var connectedState = new ConnectionConnectedState(
                            ConnectionManager,
                            new ConnectionInfo(cmd.Message),
                            cmd.Message.Error,
                            Logger
                        );
                        await ConnectionManager.SetState(connectedState);
                        break;
                    case SetConnectingStateCommand cmd:
                        var connectingState = new ConnectionConnectingState(ConnectionManager, Logger);
                        await ConnectionManager.SetState(connectingState);
                        break;
                    case SetFailedStateCommand cmd:
                        var failedState = new ConnectionFailedState(ConnectionManager, cmd.Error, Logger);
                        await ConnectionManager.SetState(failedState);
                        break;
                    case SetDisconnectedStateCommand cmd:
                        var disconnectedState = new ConnectionDisconnectedState(ConnectionManager, cmd.Error, Logger)
                            {RetryInstantly = cmd.RetryInstantly};
                        await ConnectionManager.SetState(disconnectedState, skipAttach: cmd.SkipAttach);
                        break;
                    case SetClosingStateCommand cmd:
                        var closingState = new ConnectionClosingState(ConnectionManager, Logger);
                        await ConnectionManager.SetState(closingState);
                        break;
                    case SetSuspendedStateCommand cmd:
                        var suspendedState = new ConnectionSuspendedState(ConnectionManager, cmd.Error, Logger);
                        await ConnectionManager.SetState(suspendedState);
                        break;
                    case SetClosedStateCommand cmd:
                        var closedState = new ConnectionClosedState(ConnectionManager, cmd.Error, Logger)
                            {Exception = cmd.Exception};
                        await ConnectionManager.SetState(closedState);
                        break;
                    case ProcessMessageCommand cmd:
                        await ProcessMessage(cmd.ProtocolMessage);
                        break;
                    case SendMessageCommand cmd:
                        ConnectionManager.Send(cmd.ProtocolMessage, cmd.Callback);
                        break;
                    case PingCommand cmd:
                        return HandlePingCommand(cmd);
                    case EmptyCommand _:
                        break;
                    case DelayCommand cmd:
                        DelayCommandHandler(cmd.Delay, cmd.CommandToQueue);
                        break;
                    case ListCommand listCmd:
                        return listCmd.Commands;
                    case PingTimerCommand cmd:
                        HandlePingTimer(cmd);
                        break;

                    case ChannelCommand cmd:
                        await Channels.ExecuteCommand(cmd);
                        break;
                    default:
                        throw new AblyException("No handler found for - " + command.Explain());
                }
            }
            finally
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug($"Command {command.Name}|{command.Id} completed.");
                }
            }


            async Task ProcessMessage(ProtocolMessage message)
            {
                try
                {
                    Connection.UpdateSerial(message);

                    foreach (var (name, handler) in MessageHandlers)
                    {

                        var handled = await handler(message, State);
                        if (Logger.IsDebug)
                        {
                            Logger.Debug($"Message handler '{name}' - {(handled ? "Handled" : "Skipped")}");
                        }
                        if (handled)
                        {
                            break;
                        }
                    }

                    // Notify the Channel message processor regardless of what happened above
                    await ChannelMessageProcessor.MessageReceived(message, State);
                }
                catch (Exception e)
                {
                    Logger.Error("Error processing message: " + message, e);
                    throw new AblyException(e);
                }
            }

            return Enumerable.Empty<RealtimeCommand>();
        }

        //TODO: Handle HandlePingTimer
        private void HandlePingTimer(PingTimerCommand cmd)
        {
            var relevantRequest = State.PingRequests.FirstOrDefault(x => x.Id.EqualsTo(cmd.PingRequestId));

            if (relevantRequest != null)
            {
                // fail the request if it still exists. If it was already handled it will not be there

                relevantRequest.Callback?.Invoke(null, PingRequest.TimeOutError);

                State.PingRequests.Remove(relevantRequest);
            }
        }

        private IEnumerable<RealtimeCommand> HandlePingCommand(PingCommand cmd)
        {
            if (Connection.State != ConnectionState.Connected)
            {
                // We don't want to wait for the execution to finish
                NotifyExternalClient(
                    () => { cmd.Request.Callback?.Invoke(null, PingRequest.DefaultError); },
                    "Notifying Ping callback because connection state is not Connected");
            }
            else
            {
                yield return SendMessageCommand.Create(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat)
                {
                    Id = cmd.Request.Id // Pass the ping request id so we can match it on the way back
                });

                // Only trigger the timer if there is a call back
                // Question: Do we trigger the error if there is no callback but then we can just emmit it

                if (cmd.Request.Callback != null)
                {
                    State.PingRequests.Add(cmd.Request);
                    yield return DelayCommand.Create(ConnectionManager.DefaultTimeout,
                        PingTimerCommand.Create(cmd.Request.Id));
                }
            }
        }


        private Task NotifyExternalClient(Action action, string reason)
        {
            try
            {
                return Task.Run(action);
            }
            catch (Exception e)
            {
                Logger.Error("Error while notifying external client for " + reason, e);
            }

            return Task.CompletedTask;
        }

        public void Close()
        {
            RealtimeMessageLoop.Writer.Complete();
        }
    }


    // Handle AuthUpdated
//    public void OnAuthUpdated(object sender, AblyAuthUpdatedEventArgs args)
//        {
//            if (State.State == ConnectionState.Connected)
//            {
//                /* (RTC8a) If the connection is in the CONNECTED state and
//                 * auth.authorize is called or Ably requests a re-authentication
//                 * (see RTN22), the client must obtain a new token, then send an
//                 * AUTH ProtocolMessage to Ably with an auth attribute
//                 * containing an AuthDetails object with the token string. */
//                try
//                {
//                    /* (RTC8a3) The authorize call should be indicated as completed
//                     * with the new token or error only once realtime has responded
//                     * to the AUTH with either a CONNECTED or ERROR respectively. */
//
//                    // an ERROR protocol message will fail the connection
//                    void OnFailed(object o, ConnectionStateChange change)
//                    {
//                        if (change.Current == ConnectionState.Failed)
//                        {
//                            Connection.InternalStateChanged -= OnFailed;
//                            Connection.InternalStateChanged -= OnConnected;
//                            args.CompleteAuthorization(false);
//                        }
//                    }
//
//                    void OnConnected(object o, ConnectionStateChange change)
//                    {
//                        if (change.Current == ConnectionState.Connected)
//                        {
//                            Connection.InternalStateChanged -= OnFailed;
//                            Connection.InternalStateChanged -= OnConnected;
//                            args.CompleteAuthorization(true);
//                        }
//                    }
//
//                    Connection.InternalStateChanged += OnFailed;
//                    Connection.InternalStateChanged += OnConnected;
//
//                    var msg = new ProtocolMessage(ProtocolMessage.MessageAction.Auth)
//                    {
//                        Auth = new AuthDetails { AccessToken = args.Token.Token }
//                    };
//
//                    Send(msg);
//                }
//                catch (AblyException e)
//                {
//                    Logger.Warning("OnAuthUpdated: closing transport after send failure");
//                    Logger.Debug(e.Message);
//                    Transport?.Close();
//                }
//            }
//            else
//            {
//                args.CompletedTask.TrySetResult(true);
//                Connect();
//            }
//        }
}