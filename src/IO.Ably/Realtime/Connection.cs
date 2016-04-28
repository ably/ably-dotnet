using System;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably.Realtime
{
    /// <summary>
    /// </summary>
    public class Connection : IDisposable
    {
        private readonly IConnectionManager manager;

        internal Connection()
        {
        }

        internal Connection(IConnectionManager manager)
        {
            this.manager = manager;
            State = this.manager.ConnectionState;
        }

        /// <summary>
        ///     Indicates the current state of this connection.
        /// </summary>
        public virtual ConnectionState State { get; private set; }

        /// <summary>
        ///     The id of the current connection. This string may be
        ///     used when recovering connection state.
        /// </summary>
        public virtual string Id { get; internal set; }

        /// <summary>
        ///     The serial number of the last message received on this connection.
        ///     The serial number may be used when recovering connection state.
        /// </summary>
        public virtual long Serial { get; internal set; }

        /// <summary>
        /// </summary>
        public virtual string Key { get; internal set; }

        /// <summary>
        ///     Information relating to the transition to the current state,
        ///     as an Ably ErrorInfo object. This contains an error code and
        ///     message and, in the failed state in particular, provides diagnostic
        ///     error information.
        /// </summary>
        public virtual ErrorInfo Reason { get; private set; }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// </summary>
        public virtual event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// </summary>
        public void Connect()
        {
            manager.Connect();
        }

        /// <summary>
        /// </summary>
        public Task Ping()
        {
            return manager.Ping();
        }

        /// <summary>
        ///     Causes the connection to close, entering the <see cref="ConnectionState.Closed" /> state. Once closed,
        ///     the library will not attempt to re-establish the connection without a call
        ///     to <see cref="Connect()" />.
        /// </summary>
        public void Close()
        {
            manager.Close();
        }

        internal void OnStateChanged(ConnectionState state, ErrorInfo error = null, int retryin = -1)
        {
            var oldState = State;
            State = state;
            Reason = error;
            var eh = ConnectionStateChanged;
            if (null != eh)
            {
                // TODO: Add proper arguments in Connection.ConnectionStateChanged
                eh(this, new ConnectionStateChangedEventArgs(oldState, state, retryin, error));
            }
        }
    }
}