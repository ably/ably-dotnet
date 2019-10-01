using System;
using System.Collections.Generic;

namespace IO.Ably.Transport
{
    internal sealed class ConnectionAttempt
    {
        public DateTimeOffset Time { get; }

        public List<AttemptFailedState> FailedStates { get; private set; } = new List<AttemptFailedState>();

        public ConnectionAttempt(DateTimeOffset time)
        {
            Time = time;
        }
    }
}