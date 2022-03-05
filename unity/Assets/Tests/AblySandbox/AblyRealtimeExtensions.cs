using System;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace Assets.Tests.AblySandbox
{
    public static class AblyRealtimeExtensions
    {
        public static string AddRandomSuffix(this string str)
        {
            if (str.IsEmpty())
            {
                return str;
            }

            return str + "_" + Guid.NewGuid().ToString("D").Substring(0, 8);
        }

        public static Task<TimeSpan> WaitForState(this AblyRealtime realtime, ConnectionState awaitedState, TimeSpan? waitSpan = null)
        {
            if (realtime.Connection.State == awaitedState)
            {
                return Task.FromResult(TimeSpan.Zero);
            }

            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
            {
                return connectionAwaiter.Wait(waitSpan.Value);
            }

            return connectionAwaiter.Wait();
        }

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

        public static Task WaitForState(this IRealtimeClient realtime, ConnectionState awaitedState = ConnectionState.Connected, TimeSpan? waitSpan = null)
        {
            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
            {
                return connectionAwaiter.Wait(waitSpan.Value);
            }

            return connectionAwaiter.Wait();
        }

        public static async Task ProcessMessage(this IRealtimeClient client, ProtocolMessage message)
        {
            ((AblyRealtime)client).Workflow.QueueCommand(ProcessMessageCommand.Create(message));
            await client.ProcessCommands();
        }

        /// <summary>
        /// This method yields the current thread and waits until the whole command queue is processed.
        /// </summary>
        /// <returns></returns>
        public static async Task ProcessCommands(this IRealtimeClient client)
        {
            var realtime = (AblyRealtime)client;
            var taskAwaiter = new TaskCompletionAwaiter();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(50);

                    if (realtime.Workflow.IsProcessingCommands() == false)
                    {
                        taskAwaiter.SetCompleted();
                    }
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await taskAwaiter.Task;
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
