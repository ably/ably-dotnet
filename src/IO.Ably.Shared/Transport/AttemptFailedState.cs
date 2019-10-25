using System;
using IO.Ably.Realtime;

namespace IO.Ably.Transport
{
    internal sealed class AttemptFailedState
    {
        public ErrorInfo Error { get; private set; }

        public Exception Exception { get; private set; }

        public ConnectionState State { get; private set; }

        public AttemptFailedState(ConnectionState state, ErrorInfo error)
        {
            State = state;
            Error = error;
        }

        public AttemptFailedState(ConnectionState state, Exception ex)
        {
            State = state;
            Exception = ex;
        }

        public bool ShouldUseFallback()
        {
            return IsFailedOrSuspendedState() &&
                   (IsRecoverableError() || IsRecoverableException());
        }

        private bool IsFailedOrSuspendedState()
        {
            return State == ConnectionState.Disconnected || State == ConnectionState.Suspended;
        }

        private bool IsRecoverableException()
        {
            return Exception != null;
        }

        private bool IsRecoverableError()
        {
            return Error != null && Error.IsRetryableStatusCode();
        }
    }
}
