using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Realtime.Workflow
{
    internal class RealtimeState
    {
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
        private Thread ConsumerThread;
        private Func<DateTimeOffset> Now => Connection.Now;

        internal ConnectionHeartbeatHandler HeartbeatHandler { get; }

        internal ChannelMessageProcessor ChannelMessageProcessor { get;  }

        internal List<Func<ProtocolMessage, RealtimeState, ValueTask<bool>>> MessageHandlers;

        internal readonly Channel<RealtimeCommand> RealtimeMessageLoop = Channel.CreateUnbounded<RealtimeCommand>(new UnboundedChannelOptions()
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
            MessageHandlers = new List<Func<ProtocolMessage, RealtimeState, ValueTask<bool>>>
            {
                (message, state) => ConnectionManager.State.OnMessageReceived(message, state),
                HeartbeatHandler.OnMessageReceived,
                (message, state) => ConnectionManager.AckProcessor.OnMessageReceived(message, state),
                ChannelMessageProcessor.MessageReceived,
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
                    Logger.Warning($"Cannot schedule command: {command.Explain()} because the execution channel is closed");
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
                        await ProcessCommand(cmd);
                    }
                    catch (Exception e)
                    {
                        // TODO: Figure out there will be a case where the library becomes in an invalid state
                        Logger.Error("Error processing command: " + cmd.Explain(), e);
                    }
                }
            }
        }

        private void DelayCommand(TimeSpan delay, RealtimeCommand cmd) =>
            Task.Delay(delay).ContinueWith(_ => QueueCommand(cmd));

        private async Task ProcessCommand(RealtimeCommand command)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Executing command: " + command.Explain());
            }

            switch (command)
            {
                case ConnectCommand connectCmd:
                    ConnectionManager.Connect();
                    break;
                case DisconnectCommand _:
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
                    var disconnectedState = new ConnectionDisconnectedState(ConnectionManager, cmd.Error, Logger) { RetryInstantly = cmd.RetryInstantly };
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
                    var closedState = new ConnectionClosedState(ConnectionManager, cmd.Error, Logger) { Exception = cmd.Exception };
                    await ConnectionManager.SetState(closedState);
                    break;
                case ProcessMessageCommand cmd:
                    await ProcessMessage(cmd.ProtocolMessage);
                    break;
                case PingCommand cmd:
                    await HandlePingCommand(cmd);
                    break;
                case PingTimerCommand cmd:
                    HandlePingTimer(cmd);
                    break;
                default:
                    throw new AblyException("No handler found for - " + command.Explain());
            }

            if (Logger.IsDebug)
            {
                Logger.Debug($"Command {command.Name}|{command.Id} completed: ");
            }

            async Task ProcessMessage(ProtocolMessage message)
            {
                try
                {
                    Connection.UpdateSerial(message);

                    foreach (var handler in MessageHandlers)
                    {
                        var handled = await handler(message, State);
                        if (handled)
                        {
                            break;
                        }
                    }

                    //handled |= ConnectionHeartbeatHandler.CanHandleMessage(message);
                }
                catch (Exception e)
                {
                    Logger.Error("Error processing message: " + message, e);
                    throw new AblyException(e);
                }
            }
        }

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

        private async Task HandlePingCommand(PingCommand cmd)
        {
            if (Connection.State != ConnectionState.Connected)
            {
                // We don't want to wait for the execution to finish
                NotifyExternalClient(
                    () =>
                    {
                        cmd.Request.Callback?.Invoke(null, PingRequest.DefaultError);
                    },
                    "Notifying Ping callback because connection state is not Connected");
            }
            else
            {
                SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
                // Only trigger the timer if there is a call back
                // Question: Do we trigger the error if there is no callback but then we can just emmit it


                if (cmd.Request.Callback != null)
                {
                    State.PingRequests.Add(cmd.Request);
                    DelayCommand(ConnectionManager.DefaultTimeout, PingTimerCommand.Create(cmd.Request.Id));
                }
            }
        }

        private void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback = null)
        {
            ConnectionManager.Send(message, callback);
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
}