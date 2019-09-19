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
        public List<PingRequest> PingRequests { get; set; } = new List<PingRequest>();
        public List<string> ActiveTimers { get; set; } = new List<string>();
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
        public RealtimeState State { get; }
        private Func<DateTimeOffset> Now => Connection.Now;
        internal IAcknowledgementProcessor AckHandler { get; set; }
        internal ConnectionHeartbeatHandler HeartbeatHandler { get; set; }

        internal List<Func<ProtocolMessage, ValueTask<bool>>> MessageHandlers;

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
            AckHandler = new AcknowledgementProcessor(Connection);
            MessageHandlers = new List<Func<ProtocolMessage, ValueTask<bool>>>
            {
                ConnectionManager.State.OnMessageReceived,
                AckHandler.OnMessageReceived,

                Channels.OnMessageReceived
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
                case DisconnetCommand _:
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
                    await ProcessPingTimer();
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
                        var handled = await handler(message);
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

        private async Task ProcessPingTimer()
        {
            List<PingRequest> requestsToFail = new List<PingRequest>();
            foreach (var pingRequest in State.PingRequests)
            {
                var elapsed = Now() - pingRequest.Created;
                if (elapsed > ConnectionManager.DefaultTimeout)
                {
                    requestsToFail.Add(pingRequest);
                }
            }

            foreach (var request in requestsToFail)
            {
                State.PingRequests.Remove(request);

                request.Callback?.Invoke(null, PingRequest.TimeOutError);
            }

            if(State.PingRequests.Any())
                StartSingleTimer();
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
                State.PingRequests.Add(cmd.Request);
                SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
                StartSingleTimer(Timers.PingTimer, ConnectionManager.DefaultTimeout, PingTimerCommand.Create());
            }
        }

        private void StartSingleTimer(string timerName, TimeSpan interval, RealtimeCommand timerCommand)
        {
            if (State.ActiveTimers.Contains(timerName))
            {
                return;
            }

            DelayCommand(interval, timerCommand);
            State.ActiveTimers.Add(timerName);
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
                Logger.Error("Error while notifying extrenal client for " + reason, e);
            }

            return Task.CompletedTask;
        }

    }
}