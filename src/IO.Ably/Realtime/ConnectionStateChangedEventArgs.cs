using System;

namespace IO.Ably.Realtime
{
    /// <summary>
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStateChangedEventArgs(ConnectionState previous, ConnectionState current, long retryIn,
            ErrorInfo reason)
        {
            PreviousState = previous;
            CurrentState = current;
            RetryIn = retryIn;
            Reason = reason;
        }

        /// <summary>
        /// </summary>
        public ConnectionState PreviousState { get; private set; }

        /// <summary>
        /// </summary>
        public ConnectionState CurrentState { get; private set; }

        /// <summary>
        /// </summary>
        public long RetryIn { get; private set; }

        /// <summary>
        /// </summary>
        public ErrorInfo Reason { get; private set; }
    }
}