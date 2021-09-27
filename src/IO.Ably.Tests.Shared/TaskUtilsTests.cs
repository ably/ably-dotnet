using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Tests.Infrastructure;
using Xunit;

namespace IO.Ably.Tests
{
    public class TaskUtilsTests
    {
        [Fact]
        public async Task FailingTaskShouldHaveExceptionHandled()
        {
            async Task FailingTask()
            {
                await Task.Delay(100);
                throw new Exception();
            }

            var tca = new TaskCompletionAwaiter(500);
            TaskUtils.RunInBackground(FailingTask(), exception =>
            {
                tca.SetCompleted();
            });

            var result = await tca.Task;
            result.Should().Be(!tca.TimeoutExpired);
        }
    }
}
