﻿using IO.Ably.Types;
using System;
using System.Collections.Generic;
using IO.Ably.Transport;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    /// <summary>
    ///
    /// </summary>
    public class Connection : IDisposable
    {
        internal Connection()
        {
        }

        internal Connection( IConnectionManager manager )
        {
            this.manager = manager;
            this.State = this.manager.ConnectionState;
        }

        private IConnectionManager manager;

        /// <summary>
        /// Indicates the current state of this connection.
        /// </summary>
        public virtual ConnectionState State { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public virtual event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// The id of the current connection. This string may be
        /// used when recovering connection state.
        /// </summary>
        public virtual string Id { get; internal set; }

        /// <summary>
        /// The serial number of the last message received on this connection.
        /// The serial number may be used when recovering connection state.
        /// </summary>
        public virtual long Serial { get; internal set; }

        /// <summary>
        ///
        /// </summary>
        public virtual string Key { get; internal set; }

        /// <summary>
        /// Information relating to the transition to the current state,
        /// as an Ably ErrorInfo object. This contains an error code and
        /// message and, in the failed state in particular, provides diagnostic
        /// error information.
        /// </summary>
        public virtual ErrorInfo Reason { get; private set; }

        /// <summary>
        /// </summary>
        public void Connect()
        {
            this.manager.Connect();
        }

        /// <summary>
        ///
        /// </summary>
        public Task Ping()
        {
            return this.manager.Ping();
        }

        /// <summary>
        /// Causes the connection to close, entering the <see cref="ConnectionState.Closed"/> state. Once closed,
        /// the library will not attempt to re-establish the connection without a call
        /// to <see cref="Connect()"/>.
        /// </summary>
        public void Close()
        {
            this.manager.Close();
        }

        public void Dispose()
        {
            this.Close();
        }

        internal void OnStateChanged( ConnectionState state, ErrorInfo error = null, int retryin = -1 )
        {
            ConnectionState oldState = this.State;
            this.State = state;
            this.Reason = error;
            var eh = this.ConnectionStateChanged;
            if( null != eh )
            {
                // TODO: Add proper arguments in Connection.ConnectionStateChanged
                eh( this, new ConnectionStateChangedEventArgs( oldState, state, retryin, error ) );
            }
        }
    }
}