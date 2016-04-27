using System;

namespace IO.Ably.Realtime
{
    /// <summary>
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStateChangedEventArgs(ConnectionStateType previous, ConnectionStateType current, long retryIn,
            ErrorInfo reason)
        {
            PreviousState = previous;
            CurrentState = current;
            RetryIn = retryIn;
            Reason = reason;
        }

        /// <summary>
        /// </summary>
        public ConnectionStateType PreviousState { get; private set; }

        /// <summary>
        /// </summary>
        public ConnectionStateType CurrentState { get; private set; }

        /// <summary>
        /// </summary>
        public long RetryIn { get; private set; }

        /// <summary>
        /// </summary>
        public ErrorInfo Reason { get; private set; }
    }
}