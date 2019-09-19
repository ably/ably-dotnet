using System;

namespace IO.Ably.Realtime.Workflow
{
    internal class PingRequest
    {
        public Guid Id { get; set; }

        public Action<TimeSpan?, ErrorInfo> Callback { get; }

        public PingRequest(Action<TimeSpan?, ErrorInfo> callback)
        {
            Callback = callback;
        }

    }
}