using System;
using System.Threading.Tasks;
using IO.Ably.Tests.Infrastructure;
using Xunit;

namespace IO.Ably.Tests
{
    internal static class TestHelpers
    {
        public static void AssertContainsParameter(this AblyRequest request, string key, string value)
        {
            Assert.True(
                request.QueryParameters.ContainsKey(key),
                $"Header '{key}' doesn't exist in request");
            Assert.Equal(value, request.QueryParameters[key]);
        }

        public static DateTimeOffset Now()
        {
            return NowFunc()();
        }

        public static Func<DateTimeOffset> NowFunc()
        {
            return Defaults.NowFunc();
        }

        public static async Task WaitFor(int timeoutMs, int taskCount, Action<Action> action)
        {
            var tsc = new TaskCompletionAwaiter(timeoutMs, taskCount);

            void Done()
            {
                tsc.Tick();
            }

            action(Done);
            var success = await tsc.Task;
            if (!success)
            {
                var msg = $"Timeout of {timeoutMs}ms exceeed.";
                if (taskCount > 1)
                {
                    msg += $" Completed {taskCount - tsc.TaskCount} of {taskCount} tasks.";
                }

                throw new Exception(msg);
            }
        }
    }
}
