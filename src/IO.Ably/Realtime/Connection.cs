using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    public sealed class Connection : IDisposable
    {
        private readonly IConnectionManager _manager;

        internal Connection()
        {
        }

        internal Connection(IConnectionManager manager)
        {
            _manager = manager;
            State = _manager.ConnectionState;
        }

        /// <summary>
        ///     Indicates the current state of this connection.
        /// </summary>
        public ConnectionStateType State { get; private set; }

        /// <summary>
        ///     The id of the current connection. This string may be
        ///     used when recovering connection state.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        ///     The serial number of the last message received on this connection.
        ///     The serial number may be used when recovering connection state.
        /// </summary>
        public long Serial { get; internal set; }

        /// <summary>
        /// </summary>
        public string Key { get; internal set; }

        public string RecoveryKey { get; internal set; }

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

        /// <summary>
        /// </summary>
        public void Connect()
        {
            _manager.Connect();
        }

        /// <summary>
        /// </summary>
        public Task<Result<TimeSpan?>> Ping()
        {
            return _manager.PingAsync();
        }

        /// <summary>
        ///     Causes the connection to close, entering the <see cref="ConnectionStateType.Closed" /> state. Once closed,
        ///     the library will not attempt to re-establish the connection without a call
        ///     to <see cref="Connect()" />.
        /// </summary>
        public void Close()
        {
            _manager.Close();
        }

        internal void OnStateChanged(ConnectionStateType state, ErrorInfo error = null, TimeSpan? retryin = null)
        {
            var oldState = State;
            State = state;
            Reason = error;
            var stateArgs = new ConnectionStateChangedEventArgs(oldState, state, retryin, error);
            var handler = Volatile.Read(ref ConnectionStateChanged); //Make sure we get all the subscribers on all threads
            if (Logger.IsDebug)
            {
                var delegates = handler.GetInvocationList();
                Logger.Debug($"{delegates.Length} delegates will be notified");
            }
            
            handler(this, stateArgs); 
        }
    }
}