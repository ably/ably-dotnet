using System;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Realtime.Workflow;

namespace IO.Ably.Realtime
{
    internal static class AttemptsHelpers
    {
        public static async Task<bool> CanFallback(this AblyRest restClient, ErrorInfo error)
        {
            return IsDefaultHost() &&
                   error != null && error.IsRetryableStatusCode() &&
                   await restClient.CanConnectToAbly();

            bool IsDefaultHost()
            {
                return restClient.Options.IsDefaultRealtimeHost;
            }
        }

        public static bool ShouldSuspend(this RealtimeState state, Func<DateTimeOffset> now = null)
        {
            var firstAttempt = state.AttemptsInfo.FirstAttempt;
            if (firstAttempt == null)
            {
                return false;
            }
            now = now ?? Defaults.NowFunc();
            return (now() - firstAttempt.Value) >= state.Connection.ConnectionStateTtl;
        }

        public static string GetHost(RealtimeState state, Func<string> getRealtimeHost)
        {
            var lastFailedState = state.AttemptsInfo.Attempts.SelectMany(x => x.FailedStates).LastOrDefault(x => x.ShouldUseFallback());
            string customHost = string.Empty;
            var disconnectedCount = state.AttemptsInfo.DisconnectedCount;
            var suspendedCount = state.AttemptsInfo.SuspendedCount;

            if (lastFailedState != null)
            {
                if (lastFailedState.State == ConnectionState.Disconnected)
                {
                    customHost = state.Connection.FallbackHosts[disconnectedCount % state.Connection.FallbackHosts.Count];
                }

                if (lastFailedState.State == ConnectionState.Suspended && suspendedCount > 1)
                {
                    customHost =
                        state.Connection.FallbackHosts[
                            (disconnectedCount + suspendedCount) % state.Connection.FallbackHosts.Count];
                }

                if (customHost.IsNotEmpty())
                {
                    return customHost;
                }
            }

            return getRealtimeHost();
        }
    }
}
