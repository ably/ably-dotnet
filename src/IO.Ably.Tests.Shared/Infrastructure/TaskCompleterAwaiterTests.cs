using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests.Infrastructure
{
    public class TaskCompleterAwaiterTests
    {
        [Fact]
        public void TimeoutElapsedIsInitiallyFalse()
        {
            var sut = new TaskCompletionAwaiter(500);
            sut.TimeoutExpired.Should().BeFalse();
        }

        [Fact]
        public async void TimeoutElapsedSignalsOnTimeout()
        {
            var sut = new TaskCompletionAwaiter(10);
            await Task.Delay(1000);
            sut.TimeoutExpired.Should().BeTrue();
        }
    }
}
