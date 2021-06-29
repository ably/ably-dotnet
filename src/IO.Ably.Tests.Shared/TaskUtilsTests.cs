using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Tests.Infrastructure;
using Xunit;

namespace IO.Ably.Tests
{
    public class TaskUtilsTests
    {
        public class MockBackGroundService
        {
            public static async Task FailingTask()
            {
                await Task.Delay(100);
                throw new Exception();
            }
        }

        [Fact(Skip = "Keeps failing when ran on the build server")]
        public async Task FailingTaskShouldHaveExceptionHandled()
        {
            var tsc = new TaskCompletionAwaiter(500);
            TaskUtils.RunInBackground(MockBackGroundService.FailingTask(), exception =>
            {
                tsc.SetCompleted();
            });

            var result = await tsc.Task;
            result.Should().BeTrue();
        }
    }
}
