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

            if (state.AttemptsInfo.SuspendedCount() > 0)
            {
                return true;
            }

            now = now ?? Defaults.NowFunc();
            return (now() - firstAttempt.Value) >= state.Connection.ConnectionStateTtl;
        }

        public static string GetHost(RealtimeState state, Func<string> getRealtimeHost)
        {
            var lastFailedState = state.AttemptsInfo.Attempts.SelectMany(x => x.FailedStates).LastOrDefault(x => x.ShouldUseFallback());
            string customHost = string.Empty;
            var disconnectedCount = state.AttemptsInfo.DisconnectedCount();
            var suspendedCount = state.AttemptsInfo.SuspendedCount();

            if (lastFailedState != null)
            {
                // RTN17a if we were previously connected to a fallback host
                // we need to first try again on the default host before starting to check fallback hosts
                if (state.Connection.Host.IsNotEmpty() && state.Connection.IsFallbackHost)
                {
                    return getRealtimeHost();
                }

                if (lastFailedState.State == ConnectionState.Disconnected)
                {
                    // DisconnectedCount will always be > 0 and we want to start with the first host.
                    customHost = state.Connection.FallbackHosts[(disconnectedCount - 1) % state.Connection.FallbackHosts.Count];
                }

                if (lastFailedState.State == ConnectionState.Suspended && suspendedCount > 1)
                {
                    // We -1 just to preserve the logic as above.
                    customHost =
                        state.Connection.FallbackHosts[
                            (disconnectedCount + suspendedCount - 1) % state.Connection.FallbackHosts.Count];
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
