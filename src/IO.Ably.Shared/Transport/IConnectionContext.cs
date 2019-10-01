using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal interface IConnectionContext
    {
        TimeSpan DefaultTimeout { get; }

        TimeSpan RetryTimeout { get; }

        void ExecuteCommand(RealtimeCommand cmd);

        ITransport Transport { get; }

        Connection Connection { get; }

        TimeSpan SuspendRetryTimeout { get; }

        bool ShouldWeRenewToken(ErrorInfo error, RealtimeState state);

        Task<bool> CanUseFallBackUrl(ErrorInfo error);
    }
}
