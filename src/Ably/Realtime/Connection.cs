using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    /// <summary>
    /// 
    /// </summary>
    public class Connection : IDisposable
    {
        internal Connection(IConnectionManager connection)
        {
            this.State = ConnectionState.Initialized;
            this.connection = connection;
            this.connection.StateChanged += this.ConnectionManagerStateChanged;
        }

        private IConnectionManager connection;

        /// <summary>
        /// Indicates the current state of this connection.
        /// </summary>
        public ConnectionState State { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// The id of the current connection. This string may be
        /// used when recovering connection state.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The serial number of the last message received on this connection.
        /// The serial number may be used when recovering connection state.
        /// </summary>
        public long Serial { get; private set; }

        /// <summary>
        /// Information relating to the transition to the current state,
        /// as an Ably ErrorInfo object. This contains an error code and
        /// message and, in the failed state in particular, provides diagnostic
        /// error information.
        /// </summary>
        public long Reason { get; private set; }

        /// <summary>
        /// </summary>
        public void Connect()
        {
            this.SetConnectionState(ConnectionState.Connecting);
            this.connection.Connect();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Ping()
        {
            this.connection.Ping();
        }

        /// <summary>
        /// Causes the connection to close, entering the <see cref="ConnectionState.Closed"/> state. Once closed,
        /// the library will not attempt to re-establish the connection without a call
        /// to <see cref="Connect()"/>.
        /// </summary>
        public void Close()
        {
            this.SetConnectionState(ConnectionState.Closing);
            this.connection.Close();
        }

        public void Dispose()
        {
            this.Close();
        }

        protected void SetConnectionState(ConnectionState state)
        {
            ConnectionState oldState = this.State;
            this.State = state;
            // TODO: Add proper arguments
            this.OnConnectionStateChanged(new ConnectionStateChangedEventArgs(oldState, state, -1, null));
        }

        protected void OnConnectionStateChanged(ConnectionStateChangedEventArgs args)
        {
            if (this.ConnectionStateChanged != null)
            {
                this.ConnectionStateChanged(this, args);
            }
        }

        private void ConnectionManagerStateChanged(ConnectionState newState)
        {
            this.SetConnectionState(newState);
        }
    }
}
