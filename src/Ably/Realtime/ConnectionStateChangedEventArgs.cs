using Ably.Types;
using System;

namespace Ably.Realtime
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs 
    {
        public ConnectionStateChangedEventArgs(ConnectionState previous, ConnectionState current, long retryIn, ErrorInfo reason)
        {
            this.PreviousState = previous;
            this.CurrentState = current;
            this.RetryIn = retryIn;
            this.Reason = reason;
        }

        /// <summary>
        /// 
        /// </summary>
        public ConnectionState PreviousState { get; private set; }

        /// <summary>
        /// 
        /// </summary>
		public ConnectionState CurrentState { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public long RetryIn { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ErrorInfo Reason { get; private set; }
    }
}
