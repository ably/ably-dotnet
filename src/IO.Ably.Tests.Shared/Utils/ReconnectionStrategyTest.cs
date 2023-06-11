using System.Linq;
using FluentAssertions;
using IO.Ably.Shared.Utils;
using Xunit;

namespace IO.Ably.Tests.Shared.Utils
{
    public class ReconnectionStrategyTest
    {
        [Fact]
        public void ShouldCalculateRetryTimeoutsUsingBackOffAndJitter()
        {
            var retryAttempt = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var initialTimeoutValue = 15;

            var retryTimeouts = retryAttempt.Select(attempt =>
                ReconnectionStrategy.GetRetryTime(initialTimeoutValue, attempt)).ToList();

            retryTimeouts.Distinct().Count().Should().Be(10);
            retryTimeouts.FindAll(timeout => timeout >= 30).Should().BeEmpty();

            retryTimeouts[0].Should().BeInRange(12, 15);
            retryTimeouts[1].Should().BeInRange(16, 20);
            retryTimeouts[2].Should().BeInRange(20, 25);
            for (var i = 3; i < retryTimeouts.Count; i++)
            {
                retryTimeouts[i].Should().BeInRange(24, 30);
            }
        }
    }
}
