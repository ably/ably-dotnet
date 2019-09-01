using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably
{
    public interface IProtocolMessageHandler
    {
        ValueTask<bool> OnMessageReceived(ProtocolMessage message);
    }

    internal interface IQueueCommand
    {
        void QueueCommand(params RealtimeCommand[] commands);
    }

    internal class PingRequest
    {
        public Guid Id { get; set; }
        public Action<TimeSpan?, ErrorInfo> Callback { get; }
        public PingRequest(Action<TimeSpan?, ErrorInfo> callback)
        {
            Callback = callback;
        }

    }

    internal class RealtimeState
    {

    }


    internal class RealtimeWorkflow : IQueueCommand
    {
        public Connection Connection { get; }
        public RealtimeChannels Channels { get; }

        public ConnectionManager ConnectionManager => Connection.ConnectionManager;
        public ILogger Logger { get; }
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
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                QueueCommand(cmd);
            });

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
    }

    public class AblyRealtime : IRealtimeClient
    {
        internal ILogger Logger { get; private set; }

        internal RealtimeWorkflow Workflow { get; private set; }

        private SynchronizationContext _synchronizationContext;

        public AblyRealtime(string key)
            : this(new ClientOptions(key))
        {
        }

        public AblyRealtime(ClientOptions options)
            : this(options, clientOptions => new AblyRest(clientOptions))
        {
        }

        internal AblyRealtime(ClientOptions options, Func<ClientOptions, AblyRest> createRestFunc)
        {
            Logger = options.Logger;
            CaptureSynchronizationContext(options);
            // Start Reader thread
            RestClient = createRestFunc != null ? createRestFunc.Invoke(options) : new AblyRest(options);
            Channels = new RealtimeChannels(this);
            Connection = new Connection(this, options.NowFunc, options.Logger);

            Connection.Initialise();

            Workflow = new RealtimeWorkflow(Connection, Channels, Logger);
            Workflow.Start();

            RestClient.AblyAuth.AuthUpdated += Connection.ConnectionManager.OnAuthUpdated;

            if (options.AutoConnect)
            {
                Connect();
            }
        }





        private void CaptureSynchronizationContext(ClientOptions options)
        {
            if (options.CustomContext != null)
            {
                _synchronizationContext = options.CustomContext;
            }
            else if (options.CaptureCurrentSynchronizationContext)
            {
                _synchronizationContext = SynchronizationContext.Current;
            }
        }

        public AblyRest RestClient { get; }

        public IAblyAuth Auth => RestClient.AblyAuth;

        public string ClientId => Auth.ClientId;

        internal ClientOptions Options => RestClient.Options;

        internal ConnectionManager ConnectionManager => Connection.ConnectionManager;

        /// <summary>The collection of channels instanced, indexed by channel name.</summary>
        public RealtimeChannels Channels { get; private set; }

        /// <summary>A reference to the connection object for this library instance.</summary>
        public Connection Connection { get; }

        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return RestClient.StatsAsync();
        }

        public Task<PaginatedResult<Stats>> StatsAsync(StatsRequestParams query)
        {
            return RestClient.StatsAsync(query);
        }

        public PaginatedResult<Stats> Stats()
        {
            return RestClient.Stats();
        }

        public PaginatedResult<Stats> Stats(StatsRequestParams query)
        {
            return RestClient.Stats(query);
        }

        public void Connect()
        {
            Connection.Connect();
        }

        /// <summary>
        ///     This simply calls connection.close. Causes the connection to close, entering the closed state. Once
        ///     closed, the library will not attempt to re-establish the connection without a call to connect().
        /// </summary>
        public void Close()
        {
            Connection.Close();
        }

        /// <summary>Retrieves the ably service time</summary>
        public Task<DateTimeOffset> TimeAsync()
        {
            return RestClient.TimeAsync();
        }

        internal void NotifyExternalClients(Action action)
        {
            var context = Volatile.Read(ref _synchronizationContext);
            if (context != null)
            {
                context.Post(delegate { action(); }, null);
            }
            else
            {
                action();
            }
        }
    }
}
