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
    }
}
