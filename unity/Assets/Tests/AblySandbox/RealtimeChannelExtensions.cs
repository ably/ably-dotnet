using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;

namespace Assets.Tests.AblySandbox
{
    public static class RealtimeChannelExtensions
    {
        public static async Task WaitForState(this IRealtimeChannel channel, ChannelState awaitedState = ChannelState.Attached, TimeSpan? waitSpan = null)
        {
            var channelAwaiter = new ChannelAwaiter(channel, awaitedState);
            var timespan = waitSpan.GetValueOrDefault(TimeSpan.FromSeconds(5));
            Result<bool> result = await channelAwaiter.WaitAsync(timespan);
            if (result.IsFailure)
            {
                throw new Exception($"Channel '{channel.Name}' did not reach '{awaitedState}' in {timespan.TotalSeconds} seconds. Current state {channel.State}");
            }
        }

        public static async Task<bool> WaitSync(this Presence presence, TimeSpan? waitSpan = null)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            var inProgress = presence.IsSyncInProgress;
            if (inProgress == false)
            {
                return true;
            }

            void OnPresenceOnSyncCompleted(object sender, EventArgs e)
            {
                presence.SyncCompleted -= OnPresenceOnSyncCompleted;
                taskCompletionSource.SetResult(true);
            }

            presence.SyncCompleted += OnPresenceOnSyncCompleted;
            var timeout = waitSpan ?? TimeSpan.FromSeconds(2);
            var waitTask = Task.Delay(timeout);

            // Do some waiting. Either the sync will complete or the timeout will finish.
            // if it is the timeout first then we throw an exception. This way we do go down a rabbit hole
            var firstTask = await Task.WhenAny(waitTask, taskCompletionSource.Task);
            if (waitTask == firstTask)
            {
                throw new Exception("WaitSync timed out after: " + timeout.TotalSeconds + " seconds");
            }

            return true;
        }
    }
}
