using System;
using System.Threading;
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
            var timer = new CountdownTimer("Test timer", Logger);
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
            var timer = new CountdownTimer("Test timer", Logger);
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
            void Callback()
            {
                Interlocked.Increment(ref called);
            }

            timer.Start(timeout, Callback);

            // Act
            timer.Abort();
            timer.Start(timeout, Callback);

            for (var i = 0; i < 20; i++)
            {
                if (called == 0)
                    await Task.Delay(50);
                else
                    break;
            }

            // Assert
            called.Should().Be(1);
        }

        private CountdownTimer CreateTimer()
        {
            return new CountdownTimer("Test timer", Logger);
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
            timer.Start(timeout, callback1);
            timer.Start(timeout, callback2);
            await Task.Delay(250);

            for (var i = 0; i < 20; i++)
            {
                if (called2 == 0)
                    await Task.Delay(50);
                else
                    break;
            }

            // Assert
            called2.Should().Be(1);
            called1.Should().Be(0);
        }
    }
}
