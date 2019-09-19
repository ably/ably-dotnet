using System;

namespace IO.Ably.Realtime.Workflow
{
    internal class PingRequest
    {
        public static readonly ErrorInfo DefaultError = new ErrorInfo("Unable to ping service; not connected", 40000, HttpStatusCode.BadRequest);

        public static readonly ErrorInfo TimeOutError = new ErrorInfo("Unable to ping service; Request timed out", 40800, HttpStatusCode.RequestTimeout);

        public Guid Id { get; set; }

        public Action<TimeSpan?, ErrorInfo> Callback { get; }

        public DateTimeOffset Created { get; }

        public PingRequest(Action<TimeSpan?, ErrorInfo> callback, Func<DateTimeOffset> now)
        {
            Callback = callback;
            Created = now();
        }
    }
}