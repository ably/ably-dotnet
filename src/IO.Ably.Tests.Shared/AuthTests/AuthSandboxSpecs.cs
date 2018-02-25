using FluentAssertions;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class AuthSandboxSpecs : SandboxSpecs
    {
        public AuthSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        private static TokenParams CreateTokenParams(Capability capability, TimeSpan? ttl = null)
        {
            var res = new TokenParams();
            res.ClientId = "John";
            res.Capability = capability;
            if (ttl.HasValue)
            {
                res.Ttl = ttl.Value;
            }

            return res;
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4a")] /* only tests rest so does not cover 'in the case of the realtime library, transition the connection to the FAILED state' */
        public async Task WithNoMeansToRenew_WhenTokenExpired_ShouldNotRetryAndRaiseError(Protocol protocol)
        {
            var authClient = await GetRestClient(protocol);
            var almostExpiredToken = await authClient.Auth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromSeconds(1) }, null);
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Add this to fool the client it is a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            // Trying again with the new token
            var client = await GetRestClient(protocol, options =>
            {
                options.TokenDetails = almostExpiredToken;
                options.ClientId = "123";
                options.Key = string.Empty;
            });

            client.AblyAuth.CurrentToken.IsValidToken().Should().BeTrue();

            try
            {
                client.Channels.Get("random").Publish("event", "data");
                throw new Exception("Unexpected success, the proceeding code should have raised and AblyException");
            }
            catch (AblyException e)
            {
                e.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                e.ErrorInfo.Code.Should().BeInRange(40140, 40150);
            }
        }

        /*
         * (RSA4b) When the client does have a means to renew the token automatically,
         * and the token has expired or the server has responded with a token error
         * (statusCode value of 401 and error code value in the range 40140 <= code < 40150),
         * then the client should automatically make a single attempt to reissue the token and resend the request using the new token.
         * If the token creation failed or the subsequent request with the new token failed due to a token error, then the request should result in an error
         */
        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4b")]
        public async Task AuthToken_WithMeansToRenew_WhenTokenExpired_ShouldRetry_WhenRetryFails_RaiseError(Protocol protocol)
        {
            var authClient = await GetRestClient(protocol);

            // create a tokenDetails that is about to expire
            var almostExpiredToken = await authClient.Auth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(2000) }, null);

            // Fool the client it is a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            var testLogger = new TestLogger("Handling UnAuthorized Error, attmepting to Re-authorize and repeat request.");

            // create a realtime instance with no mean to renew the token
            var restClient = await GetRestClient(protocol, (options) =>
            {
                options.TokenDetails = almostExpiredToken;
                options.ClientId = "123";
                options.Key = string.Empty;
                options.AutoConnect = false;
                options.AuthCallback = null;
                options.AuthUrl = new Uri("http://localhost:12345/invalid-uri");
                options.Logger = testLogger;
            });

            // wait for the token to expire
            await Task.Delay(TimeSpan.FromMilliseconds(2100));

            try
            {
                var channel = restClient.Channels.Get("random");
                channel.Publish("event", "data");
                throw new Exception("Unexpected success, preceeding code should have failed");
            }
            catch (AblyException e)
            {
                e.ErrorInfo.Code.Should().Be(80019);
            }

            // check the retry code was run
            testLogger.MessageSeen.Should().BeTrue();

            // only once
            testLogger.SeenCount.Should().Be(1);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c3")]
        public async Task AuthToken_WhenAuthUrlFails_ShouldRaiseError(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            client.Connect();

            // wait until connected
            await client.WaitForState();

            // set a bogus AuthUrl
            client.Options.AuthUrl = new Uri("http://localhost:8910");
            try
            {
                client.Auth.RequestToken();
                throw new Exception("Unexpected success");
            }
            catch (AblyException e)
            {
                e.ErrorInfo.Code.Should().Be(80019);
            }

            // RSA4c3 should still be connected
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c3")]
        public async Task AuthToken_WhenAuthCallbackFails_ShouldRaiseError(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            client.Connect();

            // wait until connected
            await client.WaitForState();

            client.Options.TokenDetails = new TokenDetails();
            client.Options.Key = string.Empty;
            client.Options.AuthCallback = tokenParams => throw new Exception("Force error in test");

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

            // RSA4c3 should still be connected
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c3")]
        public async Task AuthToken_WhenTokenIsInvalid_ShouldRaiseError(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            client.Connect();

            // wait until connected
            await client.WaitForState();

            // have the auth callback return an invalid token
            client.Options.AuthCallback = async tokenParams => "invalid_token";

            try
            {
                client.Auth.RequestToken();
                throw new Exception("Unexpected success");
            }
            catch (AblyException e)
            {
                e.ErrorInfo.Code.Should().Be(80019);
            }

            // RSA4c3 should still be connected
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8a")]
        public async Task ShouldReturnTheRequestedToken(Protocol protocol)
        {
            var ttl = TimeSpan.FromSeconds(30 * 60);
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await GetRestClient(protocol);
            var options = ably.Options;

            var token = await ably.Auth.RequestTokenAsync(CreateTokenParams(capability, ttl), null);

            var key = options.ParseKey();
            var appId = key.KeyName.Split('.').First();
            token.Token.Should().MatchRegex($@"^{appId}\.[\w-]+$");
            token.Issued.Should().BeWithin(TimeSpan.FromSeconds(30)).Before(DateTimeOffset.UtcNow);
            token.Expires.Should().BeWithin(TimeSpan.FromSeconds(30)).Before(DateTimeOffset.UtcNow + ttl);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA3a")]
        public async Task WithTokenId_AuthenticatesSuccessfullyOverHttpAndHttps(Protocol protocol)
        {
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await GetRestClient(protocol);
            var token = await ably.Auth.RequestTokenAsync(CreateTokenParams(capability), null);

            var options = await Fixture.GetSettings();
            var httpTokenAbly =
                new AblyRest(new ClientOptions { Token = token.Token, Environment = options.Environment, Tls = false });
            var httpsTokenAbly =
                new AblyRest(new ClientOptions { Token = token.Token, Environment = options.Environment, Tls = true });

            // If it doesn't throw we are good :)
            await httpTokenAbly.Channels.Get("foo").PublishAsync("test", "true");
            await httpsTokenAbly.Channels.Get("foo").PublishAsync("test", "true");
        }

        [Theory]
        [ProtocolData]
        public async Task WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException(Protocol protocol)
        {
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await GetRestClient(protocol);

            var token = ably.Auth.RequestTokenAsync(CreateTokenParams(capability), null).Result;

            var tokenAbly = new AblyRest(new ClientOptions { Token = token.Token, Environment = "sandbox" });

            var error =
                await
                    Assert.ThrowsAsync<AblyException>(() => tokenAbly.Channels.Get("boo").PublishAsync("test", "true"));
            error.ErrorInfo.Code.Should().Be(40160);
            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [ProtocolData]
        public async Task WithInvalidTimeStamp_Throws(Protocol protocol)
        {
            var ably = await GetRestClient(protocol);

            var error = await Assert.ThrowsAsync<AblyException>(() =>
            {
                var tokenParams = CreateTokenParams(null);
                tokenParams.Timestamp = DateTimeOffset.UtcNow.AddDays(-1);
                return ably.Auth.RequestTokenAsync(tokenParams, new AuthOptions() { QueryTime = false });
            });

            error.ErrorInfo.Code.Should().Be(40104);
            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA7a2")]
        public async Task WithClientId_RequestsATokenOnFirstMessageWithCorrectDefaults(Protocol protocol)
        {
            var ably = await GetRestClient(protocol, ablyOptions => ablyOptions.ClientId = "123");
            var channel = ably.Channels.Get("test");
            await channel.PublishAsync("test", "true");

            var token = ably.AblyAuth.CurrentToken;

            token.Should().NotBeNull();
            token.ClientId.Should().Be("123");
            token.Expires.Should()
                .BeWithin(TimeSpan.FromSeconds(20))
                .Before(DateTimeOffset.UtcNow + Defaults.DefaultTokenTtl);
            token.Capability.ToJson().Should().Be(Defaults.DefaultTokenCapability.ToJson());
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA7b2")]
        [Trait("spec", "RSA10a")]
        public async Task WithoutClientId_WhenAuthorizedWithTokenParamsWithClientId_SetsClientId(Protocol protocol)
        {
            var ably = await GetRestClient(protocol);
            await ably.Auth.AuthorizeAsync(new TokenParams() { ClientId = "123" }, new AuthOptions() { Force = true });
            ably.AblyAuth.ClientId.Should().Be("123");
            ably.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f1")]
        public async Task TokenAuthWithouthClientId_ShouldNotSetClientIdOnMessagesAndTheClient(Protocol protocol)
        {
            var client = await GetRestClient(protocol, opts => opts.QueryTime = true);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync();
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            tokenClient.AblyAuth.ClientId.Should().BeNullOrEmpty();
            var channel = tokenClient.Channels["persisted:test".AddRandomSuffix()];
            await channel.PublishAsync("test", "test");
            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().BeNullOrEmpty();
            message.Data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f2")]
        public async Task TokenAuthWithouthClientIdAndAMessageWithExplicitId_ShouldThrow(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync();
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });
            await
                Assert.ThrowsAsync<AblyException>(
                    () => tokenClient.Channels["test"].PublishAsync(new Message("test", "test") { ClientId = "123" }));
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f3")]
        public async Task TokenAuthWithWildcardClientId_ShouldPublishMessageSuccessufflyAndClientIdShouldBeSetToWildcard(
            Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = tokenClient.Channels["pesisted:test"];
            await channel.PublishAsync("test", "test");
            tokenClient.AblyAuth.ClientId.Should().Be("*");
            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().BeNullOrEmpty();
            message.Data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA8f4")]
        public async Task
            TokenAuthWithWildcardClientId_WhenPublishingMessageWithClientId_ShouldExpectClientIdToBeSentWithTheMessage(
                Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = tokenClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });
            tokenClient.AblyAuth.ClientId.Should().Be("*");
            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        public async Task TokenAuthUrlWhenPlainTextTokenIsReturn_ShouldBeAblyToPublishWithNewToken(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var settings = await Fixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=text&body=" + token.Token;

            var authUrlClient = new AblyRest(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = authUrlClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        public async Task TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToPublishWithNewToken(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var settings = await Fixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=json&body=" + Uri.EscapeUriString(token.ToJson());

            var authUrlClient = new AblyRest(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = authUrlClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        public async Task TokenAuthCallbackWithTokenDetailsReturned_ShouldBeAbleToPublishWithNewToken(Protocol protocol)
        {
            var settings = await Fixture.GetSettings();
            var tokenClient = await GetRestClient(protocol);
            var authCallbackClient = await GetRestClient(protocol, options =>
            {
                options.AuthCallback = tokenParams => tokenClient.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" }).Convert();
                options.Environment = settings.Environment;
                options.UseBinaryProtocol = protocol == Defaults.Protocol;
            });

            var channel = authCallbackClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        [Theory]
        [ProtocolData]
        public async Task TokenAuthCallbackWithTokenRequestReturned_ShouldBeAbleToGetATokenAndPublishWithNewToken(Protocol protocol)
        {
            var settings = await Fixture.GetSettings();
            var tokenClient = await GetRestClient(protocol);
            var authCallbackClient = await GetRestClient(protocol, options =>
            {
                options.AuthCallback = async tokenParams => await tokenClient.Auth.CreateTokenRequestAsync(new TokenParams() { ClientId = "*" });
                options.Environment = settings.Environment;
                options.UseBinaryProtocol = protocol == Defaults.Protocol;
            });

            var channel = authCallbackClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }
    }
}