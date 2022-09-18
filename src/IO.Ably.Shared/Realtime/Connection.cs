﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;

namespace IO.Ably.Realtime
{
    /// <summary>
    /// Represents the OS network state.
    /// </summary>
    public enum NetworkState
    {
        /// <summary>
        /// Online network state
        /// </summary>
        Online,

        /// <summary>
        /// Offline network state
        /// </summary>
        Offline
    }

    /// <summary>
    /// A class representing the connection associated with an AblyRealtime instance.
    /// The Connection object exposes the lifecycle and parameters of the realtime connection.
    /// </summary>
    public sealed class Connection : EventEmitter<ConnectionEvent, ConnectionStateChange>
    {
        private readonly Guid _objectId = Guid.NewGuid(); // Used to identify the connection object for OsEventSubscribers
        private static readonly ConcurrentDictionary<Guid, Action<NetworkState>> OsEventSubscribers =
            new ConcurrentDictionary<Guid, Action<NetworkState>>();

        /// <summary>
        /// Gets Action used to notify external clients. The logic is centralised in the RealtimeClient.
        /// </summary>
        protected override Action<Action> NotifyClient => RealtimeClient.NotifyExternalClients;

        /// <summary>
        /// This method is used when the Operating system notifies the library about changes in
        /// the network state.
        /// </summary>
        /// <param name="state">The current state of the OS network connection.</param>
        public static void NotifyOperatingSystemNetworkState(NetworkState state) =>
            NotifyOperatingSystemNetworkState(state, null);

        internal static void NotifyOperatingSystemNetworkState(NetworkState state, ILogger logger)
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
                Logger.Debug("Registering OS network state handler for Connection with id: " + _objectId.ToString("D"));
            }

            OsEventSubscribers.AddOrUpdate(_objectId, stateAction, (_, __) => stateAction);
        }

        private void CleanUpNetworkStateEvents()
        {
            try
            {
                var result = OsEventSubscribers.TryRemove(_objectId, out _);
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

        internal long MessageSerial => InnerState.MessageSerial;

        /// <summary>
        /// Gets the current connection Key.
        /// </summary>
        public string Key => InnerState.Key;

        // /// <summary>
        // /// - (RTN16b) Connection#recoveryKey is an attribute composed of the connectionKey, and the latest connectionSerial received on the connection, and the current msgSerial.
        // /// </summary>
        // public string RecoveryKey
        // {
        //     get
        //     {
        //         Debug.Assert(Serial.HasValue, "Expected a Value, found none");
        //         return ConnectionResumable ? $"{Key}:{Serial.Value}:{MessageSerial}" : string.Empty;
        //     }
        // }

        /// <summary>
        /// Gets the current connections time to live.
        /// </summary>
        public TimeSpan ConnectionStateTtl => InnerState.ConnectionStateTtl;

        /// <summary>
        ///     Information relating to the transition to the current state,
        ///     as an Ably ErrorInfo object. This contains an error code and
        ///     message and, in the failed state in particular, provides diagnostic
        ///     error information.
        /// </summary>
        public ErrorInfo ErrorReason => InnerState.ErrorReason;

        /// <summary>
        /// Gets the currently used Host.
        /// </summary>
        public string Host => InnerState.Host;

        /// <summary>
        /// Gets the last point in time when the connection was confirmed alive.
        /// </summary>
        public DateTimeOffset? ConfirmedAliveAt => InnerState.ConfirmedAliveAt;

        internal void RemoveAllListeners()
        {
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

        /// <summary>
        /// EventHandler which exposes Connection state changes in an idiomatic .net way.
        /// </summary>
        public event EventHandler<ConnectionStateChange> ConnectionStateChanged = delegate { };

        // Not be used for testing. Tests should depend on functionality only available to public clients
        internal event EventHandler<ConnectionStateChange> InternalStateChanged = delegate { };

        /// <summary>
        /// Instructs the library to start a connection to the server.
        /// </summary>
        public void Connect()
        {
            ThrowIfDisposed();

            ExecuteCommand(ConnectCommand.Create().TriggeredBy("Connection.Connect()"));
        }

        /// <summary>
        /// Sends a ping request to the server and waits for the requests to be confirmed.
        /// </summary>
        /// <returns>returns a result which is either successful and has the time it took for the request to complete or
        /// it contains an error indicating the reason of the failure.</returns>
        public Task<Result<TimeSpan?>> PingAsync()
        {
            ThrowIfDisposed();
            return TaskWrapper.Wrap<TimeSpan?>(Ping);
        }

        /// <summary>
        /// Sends a ping request to the server. It supports an optional <paramref name="callback"/> parameter if the
        /// client wants to be notified of the result. Use <methodref name="PingAsync"/> inside async methods.
        /// </summary>
        /// <param name="callback">an action which will be called when the Ping completes. It either indicates success or contains an error.</param>
        public void Ping(Action<TimeSpan?, ErrorInfo> callback)
        {
            ThrowIfDisposed();

            ExecuteCommand(PingCommand.Create(new PingRequest(callback, Now)).TriggeredBy("Connection.Ping()"));
        }

        private void ExecuteCommand(RealtimeCommand cmd)
        {
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
            ExecuteCommand(CloseConnectionCommand.Create().TriggeredBy("Connection.Close()"));
        }

        internal void NotifyUpdate(ConnectionStateChange stateChange)
        {
            var externalHandlers =
                Volatile.Read(ref ConnectionStateChanged); // Make sure we get all the subscribers on all threads

            var internalHandlers = Volatile.Read(ref InternalStateChanged);

            internalHandlers(this, stateChange);

            RealtimeClient.NotifyExternalClients(
                () =>
                {
                        Emit(stateChange.Event, stateChange);
                        try
                        {
                            externalHandlers?.Invoke(this, stateChange);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error notifying Connection state changed handlers", ex);
                        }
                });
        }

        private void ThrowIfDisposed()
        {
            if (RealtimeClient.Disposed)
            {
                throw new ObjectDisposedException("The ably realtime instance has been disposed. Please create a new one.");
            }
        }
    }
}
