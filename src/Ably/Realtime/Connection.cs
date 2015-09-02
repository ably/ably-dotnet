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
        internal Connection(IConnectionManager manager)
        {
            this.State = ConnectionState.Initialized;
            this.manager = manager;
        }

        private IConnectionManager manager;

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
        public string Id { get; internal set; }

        /// <summary>
        /// The serial number of the last message received on this connection.
        /// The serial number may be used when recovering connection state.
        /// </summary>
        public long Serial { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public string Key { get; internal set; }

        /// <summary>
        /// Information relating to the transition to the current state,
        /// as an Ably ErrorInfo object. This contains an error code and
        /// message and, in the failed state in particular, provides diagnostic
        /// error information.
        /// </summary>
        public ErrorInfo Reason { get; private set; }

        /// <summary>
        /// </summary>
        public void Connect()
        {
            this.manager.Connect();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Ping()
        {
            this.Ping(null);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Ping(Action<bool, ErrorInfo> callback)
        {
            this.manager.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat), callback);
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

        internal void OnStateChanged(ConnectionState state, ErrorInfo error = null)
        {
            ConnectionState oldState = this.State;
            this.State = state;
            this.Reason = error;

            // TODO: Add proper arguments in Connection.ConnectionStateChanged
            if (this.ConnectionStateChanged != null)
            {
                this.ConnectionStateChanged(this, new ConnectionStateChangedEventArgs(oldState, state, -1, error));
            }
        }
    }
}
