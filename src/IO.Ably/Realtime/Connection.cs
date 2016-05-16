using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;

namespace IO.Ably.Realtime
{
    public sealed class Connection : IDisposable
    {
        internal AblyRest RestClient { get; }
        internal ConnectionManager ConnectionManager { get; set; }

        internal Connection(AblyRest restClient)
        {
            RestClient = restClient;
        }

        internal void Initialise()
        {
            ConnectionManager = new ConnectionManager(this);
            ConnectionState = new ConnectionInitializedState(ConnectionManager);
        }

        /// <summary>
        ///     Indicates the current state of this connection.
        /// </summary>
        public ConnectionStateType State => ConnectionState.State;

        internal ConnectionState ConnectionState { get; set; }

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
        public ErrorInfo Reason { get; private set; }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged = delegate { };
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
            ConnectionHeartbeatRequest.Execute(ConnectionManager, callback);
        }


        /// <summary>
        ///     Causes the connection to close, entering the <see cref="ConnectionStateType.Closed" /> state. Once closed,
        ///     the library will not attempt to re-establish the connection without a call
        ///     to <see cref="Connect()" />.
        /// </summary>
        public void Close()
        {
            ConnectionManager.CloseConnection();
        }

        internal void UpdateState(ConnectionState state)
        {
            if (state.State == State)
                return;

            if (Logger.IsDebug)
            {
                Logger.Debug($"Connection notifying subscribers for state change `{state.State}`");
            }
            var oldState = ConnectionState.State;
            var newState = state.State;
            ConnectionState = state;
            Reason = state.Error;
            var stateArgs = new ConnectionStateChangedEventArgs(oldState, newState, state.RetryIn, Reason);

            //TODO: Execute this not on the Connection Manager's thread
            var handler = Volatile.Read(ref ConnectionStateChanged); //Make sure we get all the subscribers on all threads
            handler(this, stateArgs); 
        }
    }
}