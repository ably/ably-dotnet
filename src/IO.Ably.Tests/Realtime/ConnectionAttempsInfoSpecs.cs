﻿using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ConnectionAttemptsInfoSpecs : MockHttpRealtimeSpecs
    {
        private readonly Connection _connection;
        private readonly ConnectionAttemptsInfo _info;
        private bool _internetCheckOK = true;

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
            _info.Attempts.Add(new ConnectionAttempt(Config.Now()));
            //Move now to default ConnetionStatettl - 1 second
            Now = Now.Add(Defaults.ConnectionStateTtl.Add(TimeSpan.FromSeconds(-1)));
            _info.ShouldSuspend().Should().BeFalse();
        }
        [Fact]
        public void ShouldSuspend_WhenFirstAttemptEqualOrGreaterThanConnectionStateTtl_ShouldReturnTrue()
        {
            _info.Attempts.Add(new ConnectionAttempt(Config.Now()));
            //Move now to default ConnetionStatettl - 1 second
            Now = Now.Add(Defaults.ConnectionStateTtl);
            _info.ShouldSuspend().Should().BeTrue(); // =
            Now = Now.AddSeconds(1);
            _info.ShouldSuspend().Should().BeTrue(); // >
        }

        [Fact]
        public async Task CanAttemptFallback_ShouldBeFalseWithNonDefaultHost()
        {
            var info = Create(opts => opts.RealtimeHost = "test.test.com");

            var result = await info.CanFallback(null);
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(500)]
        [InlineData(501)]
        [InlineData(502)]
        [InlineData(503)]
        [InlineData(504)]
        public async Task CanAttemptFallback_WithDefaultHostAndAppropriateError_ShouldBeTrue(int httpcode)
        {
            var info = Create();

            var result = await info.CanFallback(new ErrorInfo("test", 111, (HttpStatusCode)httpcode));
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanAttemptFallback_WhenInternetCheckFails_ShouldBeFalse()
        {
            var info = Create();
            _internetCheckOK = false;
            var result = await info.CanFallback(null);
            result.Should().BeFalse();
        }

        private ConnectionAttemptsInfo Create(Action<ClientOptions> optionsAction = null)
        {
            return new ConnectionAttemptsInfo(new Connection(GetRealtime(optionsAction)));
        }

        public ConnectionAttemptsInfoSpecs(ITestOutputHelper output) : base(output)
        {
            _connection = new Connection(GetRealtime());
            _info = new ConnectionAttemptsInfo(_connection);
        }

        private AblyRealtime GetRealtime(Action<ClientOptions> optionsAction = null)
        {
            return GetRealtimeClient(request =>
            {
                if (request.Url == Defaults.InternetCheckURL)
                {
                    return (_internetCheckOK ? Defaults.InternetCheckOKMessage : "Blah").ToAblyResponse();
                }
                return DefaultResponse.ToTask();
            }, optionsAction);
        }
    }
}
