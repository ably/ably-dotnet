using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using IO.Ably.Shared.Utils;
using Xunit;

namespace IO.Ably.Tests.Shared.Utils
{
    public class ReconnectionStrategyTest
    {
        [Fact]
        public void ShouldCalculateRetryTimeInterval()
        {
            var retryCounts = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var initialTimeoutValue = 15;

            var retryTimeouts = retryCounts.Select(retryCount =>
                ReconnectionStrategy.GetRetryTime(initialTimeoutValue, retryCount)).ToList();

            retryTimeouts.Distinct().Count().Should().Be(10);
            retryTimeouts.FindAll(timeout => timeout >= 30).Should().BeEmpty();
        }
    }
}
