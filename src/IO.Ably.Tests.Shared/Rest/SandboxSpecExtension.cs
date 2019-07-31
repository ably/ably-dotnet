using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably.Tests
{
    public static class SandboxSpecExtension
    {
        internal static Task WaitForState(this AblyRealtime realtime, ConnectionState awaitedState = ConnectionState.Connected, TimeSpan? waitSpan = null)
        {
            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
            {
                return connectionAwaiter.Wait(waitSpan.Value);
            }

            return connectionAwaiter.Wait();
        }

        internal static Task WaitForState(this IRealtimeChannel channel, ChannelState awaitedState = ChannelState.Attached, TimeSpan? waitSpan = null)
        {
            var channelAwaiter = new ChannelAwaiter(channel, awaitedState);
            if (waitSpan.HasValue)
            {
                return channelAwaiter.WaitAsync();
            }

            return channelAwaiter.WaitAsync();
        }

        internal static async Task<bool> WaitSync(this Presence presence, TimeSpan? waitSpan = null)
        {
            TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
            var inProgress = presence.IsSyncInProgress;
            if (inProgress == false)
            {
                return true;
            }

            void OnPresenceOnSyncCompleted(object sender, EventArgs e)
            {
                presence.SyncCompleted -= OnPresenceOnSyncCompleted;
                _taskCompletionSource.SetResult(true);
            }

            presence.SyncCompleted += OnPresenceOnSyncCompleted;
            var timeout = waitSpan ?? TimeSpan.FromSeconds(2);
            var waitTask = Task.Delay(timeout);

            // Do some waiting. Either the sync will complete or the timeout will finish.
            // if it is the timeout first then we throw an exception. This way we do go down a rabbit hole
            var firstTask = await Task.WhenAny(waitTask, _taskCompletionSource.Task);
            if (waitTask == firstTask)
            {
                throw new Exception("Waitsync timed out after: " + timeout.TotalSeconds + " seconds");
            }

            return true;
        }

    }
}