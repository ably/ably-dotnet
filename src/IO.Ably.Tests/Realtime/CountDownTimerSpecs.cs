using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Transport.States.Connection;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class CountDownTimerSpecs : AblySpecs
    {
        public CountDownTimerSpecs(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        [Trait("intermittent", "true")]
        public async Task CountdownTimer_Start_StartsCountdown()
        {
            // Arrange
            var timer = new CountdownTimer("Test timer");
            var timeout = TimeSpan.FromMilliseconds(10);
            int called = 0;
            Action callback = () => called++;

            // Act
            timer.Start(timeout, callback);
            await Task.Delay(50);

            // Assert
            called.Should().Be(1);
        }

        [Fact]
        public async Task CountdownTimer_Abort_StopsCountdown()
        {
            // Arrange
            var timer = new CountdownTimer("Test timer");
            var timeout = TimeSpan.FromMilliseconds(10);
            int called = 0;
            Action callback = () => called++;
            timer.Start(timeout, callback);

            // Act
            timer.Abort();
            await Task.Delay(50);

            // Assert
            called.Should().Be(0);
        }

        [Fact]
        public async Task CountdownTimer_AbortStart_StartsNewCountdown()
        {
            // Arrange
            var timer = CreateTimer();

            var timeout = TimeSpan.FromMilliseconds(10);
            int called = 0;
            Action callback = () => called++;
            timer.Start(timeout, callback);

            // Act
            timer.Abort();
            timer.Start(timeout, callback);
            await Task.Delay(50);

            // Assert
            called.Should().Be(1);
        }

        private static CountdownTimer CreateTimer()
        {
            return new CountdownTimer("Test timer");
        }

        [Fact]
        [Trait("intermittent", "true")]
        public async Task CountdownTimer_StartTwice_AbortsOldTimer()
        {
            // Arrange
            CountdownTimer timer = CreateTimer();
            var timeout = TimeSpan.FromMilliseconds(10);
            int called1 = 0;
            int called2 = 0;
            Action callback1 = () =>
            {
                called1++;
            };
            Action callback2 = () =>
            {
                called2++;
            };

            // Act
            timer.Start(timeout, callback1, true);
            timer.Start(timeout, callback2, true);
            await Task.Delay(50);

            // Assert
            called2.Should().BeGreaterThan(1);
            called1.Should().Be(0);
        }
    }
}
