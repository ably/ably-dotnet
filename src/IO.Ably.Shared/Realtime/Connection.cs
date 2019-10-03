using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public enum NetworkState
    {
        Online,
        Offline
    }

    public sealed class Connection : EventEmitter<ConnectionEvent, ConnectionStateChange>, IDisposable
    {
        private readonly Guid ObjectId = Guid.NewGuid(); //Used to identify the connection object for OsEventSubscribers
        private static readonly ConcurrentDictionary<Guid, Action<NetworkState>> OsEventSubscribers =
            new ConcurrentDictionary<Guid, Action<NetworkState>>();

        protected override Action<Action> NotifyClient => RealtimeClient.NotifyExternalClients;

        internal static void NotifyOperatingSystemNetworkState(NetworkState state, ILogger logger = null)
        {
            if (logger == null)
            {
                logger = DefaultLogger.LoggerInstance;
            }

            if (logger.IsDebug)
            {
                logger.Debug("OS Network connection state: " + state);
            }

            foreach (var subscriber in OsEventSubscribers.ToArray())
            {
                try
                {
                    if (logger.IsDebug)
                    {
                        logger.Debug("Calling network state handler for connection with id: " + subscriber.Key.ToString("D"));
                    }

                    subscriber.Value?.Invoke(state);
                }
                catch (Exception e)
                {
                    logger.Error($"Error notifying connectionId {subscriber.Key:D} about network events", e);
                }
            }
        }

        private void RegisterWithOsNetworkStateEvents(Action<NetworkState> stateAction)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Registering OS network state handler for Connection with id: " + ObjectId.ToString("D"));
            }

            OsEventSubscribers.AddOrUpdate(ObjectId, stateAction, (_, __) => stateAction);
        }

        private void CleanUpNetworkStateEvents()
        {
            try
            {
                var result = OsEventSubscribers.TryRemove(ObjectId, out _);
                if (Logger.IsDebug)
                {
                    Logger.Debug("Os network listener removed result: " + result);
                }

            }
            catch (Exception)
            {
                Logger.Warning("Error cleaning up networking events hook");
            }
        }

        internal AblyRest RestClient => RealtimeClient.RestClient;

        internal AblyRealtime RealtimeClient { get; }

        internal ConnectionManager ConnectionManager { get; set; }

        internal Func<DateTimeOffset> Now { get; set; }

        internal bool CanPublishMessages =>
            State == Realtime.ConnectionState.Connected
            || ((State == Realtime.ConnectionState.Initialized
                 || State == Realtime.ConnectionState.Connecting
                 || State == Realtime.ConnectionState.Disconnected)
                && RealtimeClient.Options.QueueMessages);

        internal Connection(AblyRealtime realtimeClient, Func<DateTimeOffset> nowFunc, ILogger logger = null)
            : base(logger)
        {
            Now = nowFunc;
            RealtimeClient = realtimeClient;

            RegisterWithOsNetworkStateEvents(HandleNetworkStateChange);
        }

        internal RealtimeState.ConnectionData InnerState => RealtimeClient.State.Connection;

        internal void Initialise()
        {
            ConnectionManager = new ConnectionManager(this, Now, Logger);
        }

        /// <summary>
        ///     Indicates the current state of this connection.
        /// </summary>
        public ConnectionState State => ConnectionState.State;

        internal NetworkState NetworkState { get; set; } = NetworkState.Online;

        private void HandleNetworkStateChange(NetworkState state)
        {
            NetworkState = state;
            ConnectionManager.HandleNetworkStateChange(state);
        }

        internal ConnectionStateBase ConnectionState => InnerState.CurrentStateObject;

        /// <summary>
        ///     The id of the current connection. This string may be
        ///     used when recovering connection state.
        /// </summary>
        public string Id => InnerState.Id;

        /// <summary>
        ///     The serial number of the last message received on this connection.
        ///     The serial number may be used when recovering connection state.
        /// </summary>
        public long? Serial => InnerState.Serial;

        internal long MessageSerial => InnerState.MessageSerial;

        /// <summary>
        /// </summary>
        public string Key => InnerState.Key;

        public bool ConnectionResumable => Key.IsNotEmpty() && Serial.HasValue;

        /// <summary>
        /// - (RTN16b) Connection#recoveryKey is an attribute composed of the connectionKey, and the latest connectionSerial received on the connection, and the current msgSerial
        /// </summary>
        public string RecoveryKey => ConnectionResumable ? $"{Key}:{Serial.Value}:{MessageSerial}" : string.Empty;

        public TimeSpan ConnectionStateTtl => InnerState.ConnectionStateTtl;

        /// <summary>
        ///     Information relating to the transition to the current state,
        ///     as an Ably ErrorInfo object. This contains an error code and
        ///     message and, in the failed state in particular, provides diagnostic
        ///     error information.
        /// </summary>
        public ErrorInfo ErrorReason => InnerState.ErrorReason;

        public string Host => InnerState.Host;
        public DateTimeOffset? ConfirmedAliveAt => InnerState.ConfirmedAliveAt;

        public void Dispose()
        {
            Close();
            ClearAllDelegatesOfStateChangeEventHandler();
            CleanUpNetworkStateEvents();
            Off();
        }

        private void ClearAllDelegatesOfStateChangeEventHandler()
        {
            foreach (var handler in ConnectionStateChanged.GetInvocationList())
            {
                ConnectionStateChanged -= (EventHandler<ConnectionStateChange>)handler;
            }
        }

        public event EventHandler<ConnectionStateChange> ConnectionStateChanged = delegate { };

        // Not be used for testing. Tests should depend on functionality only available to public clients
        internal event EventHandler<ConnectionStateChange> InternalStateChanged = delegate { };

        public void Connect()
        {
            ExecuteCommand(ConnectCommand.Create());
        }

        public Task<Result<TimeSpan?>> PingAsync()
        {
            return TaskWrapper.Wrap<TimeSpan?>(Ping);
        }

        public void Ping(Action<TimeSpan?, ErrorInfo> callback)
        {
            ExecuteCommand(new PingCommand(new PingRequest(callback, Now)));
        }

        private void ExecuteCommand(RealtimeCommand cmd)
        {
            if (RealtimeClient.Disposed)
            {
                throw new ObjectDisposedException("This instance has been disposed. Please create a new one.");
            }

            // Find a better way to reference the workflow
            RealtimeClient.Workflow.QueueCommand(cmd);
        }

        /// <summary>
        ///     Causes the connection to close, entering the <see cref="Realtime.ConnectionState.Closed" /> state. Once closed,
        ///     the library will not attempt to re-establish the connection without a call
        ///     to <see cref="Connect()" />.
        /// </summary>
        public void Close()
        {
            ExecuteCommand(CloseConnectionCommand.Create());
        }

        internal void NotifyUpdate(ConnectionStateChange stateChange)
        {
            var externalHandlers =
                Volatile.Read(ref ConnectionStateChanged); // Make sure we get all the subscribers on all threads

            var internalHandlers = Volatile.Read(ref InternalStateChanged);

            internalHandlers(this, stateChange);

            RealtimeClient.NotifyExternalClients(
                () => {
                        Emit(stateChange.Event, stateChange);
                        try
                        {
                            externalHandlers(this, stateChange);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error notifying Connection state changed handlers", ex);
                        }});
        }
    }
}
