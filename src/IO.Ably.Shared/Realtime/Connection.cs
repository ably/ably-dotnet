using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably;
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

    public sealed class Connection : EventEmitter<ConnectionState, ConnectionStateChange>, IDisposable
    {
        private static readonly ConcurrentBag<WeakReference<Action<NetworkState>>> OsEventSubscribers = new ConcurrentBag<WeakReference<Action<NetworkState>>>();

        protected override Action<Action> NotifyClient => RealtimeClient.NotifyExternalClients;

        internal static void NotifyOperatingSystemNetworkState(NetworkState state, ILogger logger)
        {
            if(logger.IsDebug) logger.Debug("OS Network connection state: " + state);

            foreach (var subscriber in OsEventSubscribers.ToArray())
            {
                Action<Realtime.NetworkState> stateAction = null;
                if (subscriber.TryGetTarget(out stateAction))
                    stateAction?.Invoke(state);
            }
        }

        private void RegisterWithOSNetworkStateEvents(Action<NetworkState> stateAction)
        {
            OsEventSubscribers.Add(new WeakReference<Action<NetworkState>>(stateAction));
        }


        internal AblyRest RestClient => RealtimeClient.RestClient;
        internal AblyRealtime RealtimeClient { get; }
        internal ConnectionManager ConnectionManager { get; set; }
        internal List<string> FallbackHosts;
        internal ChannelMessageProcessor ChannelMessageProcessor { get; set; }
        private string _host;

        internal Func<DateTimeOffset> Now { get; set; }

        internal Connection(AblyRealtime realtimeClient, Func<DateTimeOffset> nowFunc, ILogger logger = null) : base(logger)
        {
            Now = nowFunc;
            FallbackHosts = Defaults.FallbackHosts.Shuffle().ToList();
            RealtimeClient = realtimeClient;
            RegisterWithOSNetworkStateEvents(HandleNetworkStateChange);
        }

        internal void Initialise()
        {
            ConnectionManager = new ConnectionManager(this, Now, Logger);
            ChannelMessageProcessor = new ChannelMessageProcessor(ConnectionManager, RealtimeClient.Channels);
            ConnectionState = new ConnectionInitializedState(ConnectionManager, Logger);
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

        internal ConnectionStateBase ConnectionState { get; set; }

        /// <summary>
        ///     The id of the current connection. This string may be
        ///     used when recovering connection state.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        ///     The serial number of the last message received on this connection.
        ///     The serial number may be used when recovering connection state.
        /// </summary>
        public long? Serial { get; internal set; }

        internal long MessageSerial { get; set; } = 0;
        /// <summary>
        /// </summary>
        public string Key { get; internal set; }

        public bool ConnectionResumable => Key.IsNotEmpty() && Serial.HasValue;

        public string RecoveryKey => ConnectionResumable ? $"{Key}:{Serial.Value}" : "";

        public TimeSpan ConnectionStateTtl { get; internal set; } = Defaults.ConnectionStateTtl;
        /// <summary>
        ///     Information relating to the transition to the current state,
        ///     as an Ably ErrorInfo object. This contains an error code and
        ///     message and, in the failed state in particular, provides diagnostic
        ///     error information.
        /// </summary>
        public ErrorInfo ErrorReason { get; private set; }

        public string Host
        {
            get { return _host; }
            internal set
            {
                _host = value;
                RestClient.CustomHost = FallbackHosts.Contains(_host) ? _host : "";
            }
        }

        public void Dispose()
        {
            Close();
            ClearAllDelegatesOfStateChangeEventHandler();
        }

        private void ClearAllDelegatesOfStateChangeEventHandler()
        {
            foreach (var d in InternalStateChanged.GetInvocationList())
                InternalStateChanged -= (EventHandler<ConnectionStateChange>) d;

            foreach (var handler in ConnectionStateChanged.GetInvocationList())
                ConnectionStateChanged -= (EventHandler<ConnectionStateChange>) handler;
        }

        /// <summary>
        /// </summary>
        internal event EventHandler<ConnectionStateChange> InternalStateChanged = delegate { };
        public event EventHandler<ConnectionStateChange> ConnectionStateChanged = delegate { };
        //TODO: Add IDisposable and clear all event hadlers when the connection is disposed

        /// <summary>
        /// </summary>
        public void Connect()
        {
            ConnectionManager.Connect();
        }

        /// <summary>
        /// </summary>
        public Task<Result<TimeSpan?>> PingAsync()
        {
            return TaskWrapper.Wrap<TimeSpan?>(Ping);
        }

        public void Ping(Action<TimeSpan?, ErrorInfo> callback)
        {
            ConnectionHeartbeatRequest.Execute(ConnectionManager, Now, callback);
        }

        /// <summary>
        ///     Causes the connection to close, entering the <see cref="Realtime.ConnectionState.Closed" /> state. Once closed,
        ///     the library will not attempt to re-establish the connection without a call
        ///     to <see cref="Connect()" />.
        /// </summary>
        public void Close()
        {
            ConnectionManager.CloseConnection();
        }

        internal void UpdateState(ConnectionStateBase state)
        {
            if (state.State == State)
                return;

            if (Logger.IsDebug) Logger.Debug($"Connection notifying subscribers for state change `{state.State}`");

            var oldState = ConnectionState.State;
            var newState = state.State;
            ConnectionState = state;
            ErrorReason = state.Error;
            var stateChange = new ConnectionStateChange(oldState, newState, state.RetryIn, ErrorReason);

            var internalHandlers = Volatile.Read(ref InternalStateChanged); //Make sure we get all the subscribers on all threads
            var externalHandlers = Volatile.Read(ref ConnectionStateChanged); //Make sure we get all the subscribers on all threads
            internalHandlers(this, stateChange);

            RealtimeClient.NotifyExternalClients(() =>
            {
                try
                {
                    externalHandlers(this, stateChange);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error notifying Connection state changed handlers", ex);
                }

                Emit(newState, stateChange);
            });
        }

        public void UpdateSerial(ProtocolMessage message)
        {
            if (message.ConnectionSerial.HasValue)
            {
                Serial = message.ConnectionSerial.Value;
            }
        }

    }
}