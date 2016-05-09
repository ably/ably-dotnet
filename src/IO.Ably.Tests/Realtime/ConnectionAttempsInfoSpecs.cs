using System;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ConnectionAttemptsInfoSpecs : AblySpecs
    {
        private Connection _connection;
        private ClientOptions _options;
        private ConnectionAttemptsInfo _info;

        [Fact]
        public void FirstIncrement_ShouldSetCurrentTimeAndIncrementAttempts()
        {
            _info.Increment();
            _info.FirstAttempt.Should().Be(Now);
            _info.NumberOfAttempts.Should().Be(1);
        }

        [Fact]
        public void Reset_SHouldResetFirstAttemptAndNumberOfAttempts()
        {
            _info.Reset();
            _info.FirstAttempt.Should().NotHaveValue();
            _info.NumberOfAttempts.Should().Be(0);
        }

        [Fact]
        public void ShouldSuspend_WhenConnectionHasNotBeenAttempted_ShouldReturnFalse()
        {
            _info.ShouldSuspend().Should().BeFalse();
        }

        [Fact]
        public void ShouldSuspend_WhenFirstAttemptIsWithinConnectionStateTtl_ShouldReturnFalse()
        {
            _info.Increment();
            //Move now to default ConnetionStatettl - 1 second
            Now = Now.Add(Defaults.ConnectionStateTtl.Add(TimeSpan.FromSeconds(-1)));
            _info.ShouldSuspend().Should().BeFalse();
        }
        [Fact]
        public void ShouldSuspend_WhenFirstAttemptEqualOrGreaterThanConnectionStateTtl_ShouldReturnTrue()
        {
            _info.Increment();
            //Move now to default ConnetionStatettl - 1 second
            Now = Now.Add(Defaults.ConnectionStateTtl);
            _info.ShouldSuspend().Should().BeTrue(); // =
            Now = Now.AddSeconds(1);
            _info.ShouldSuspend().Should().BeTrue(); // >

        }



        public ConnectionAttemptsInfoSpecs(ITestOutputHelper output) : base(output)
        {
            _connection = new Connection();
            _options = new ClientOptions();
            _info = new ConnectionAttemptsInfo(_options, _connection);
        }
    }
}
