using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Events;
using IO.Ably.Encryption;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Transport.States.Connection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("Realtime SandBox")]
    [Trait("requires", "sandbox")]
    public class RealtimeSandboxSpecs : SandboxSpecs
    {
        public RealtimeSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output){}

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4a")]
        public async Task AuthToken_WithNoMeansToRenew_WhenTokenExpired_ShouldNotRetryAndRaiseError(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            // Arrange

            var authClient = await GetRestClient(protocol);
            var almostExpiredToken = await authClient.Auth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromSeconds(1) }, null);
            await Task.Delay(TimeSpan.FromSeconds(2));

            //Add this to fool the client it is a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            var client = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = almostExpiredToken;
                options.ClientId = "123";
                options.Key = "";
                options.AutoConnect = false;
            });

            bool caught = false;
            try
            {
                client.Connect();
                var channel = client.Channels.Get("random");
                channel.Publish("event", "data");
            }
            catch (AblyException e)
            {
                caught = true;
                e.ErrorInfo.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
                e.ErrorInfo.Code.Should().BeInRange(40140, 40150);
            }
            catch (Exception e)
            {
                throw e;
            }
            caught.Should().BeTrue();
        }


        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        public async Task AuthToken_WhenAuthUrlFails_ShouldNotRetryAndRaiseError(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            // Arrange
            

            var client = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = new TokenDetails();
                options.ClientId = "123";
                options.Key = "";
                options.AuthUrl = new Uri("http://localhost:8910");
            });

            
            try
            {
                client.Auth.RequestToken();
                throw new Exception("Unexpected success");
            }
            catch (AblyException e)
            {
                e.ErrorInfo.Code.Should().Be(80019);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}
