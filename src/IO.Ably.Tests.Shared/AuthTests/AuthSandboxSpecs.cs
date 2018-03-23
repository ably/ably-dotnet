using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Shared;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class AuthSandboxSpecs : SandboxSpecs
    {
        private string _invalidAuthUrl = "http://domain-that-goes-nowhere.local:12345/";

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
        public async Task RSA4Helper_RestClient_ShouldTrackRequests(Protocol protocol)
        {
            var authClient = await GetRestClient(protocol);
            var token = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123" });
            var helper = new RSA4Helper(this);
            var restClient = await helper.GetRestClientWithRequests(protocol, token, invalidateKey: true);
            helper.Requests.Count.Should().Be(0);
            await restClient.TimeAsync();
            helper.Requests.Count.Should().Be(1);
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, token, invalidateKey: true);
            helper.Requests.Count.Should().Be(1);
            await realtimeClient.RestClient.TimeAsync();
            helper.Requests.Count.Should().Be(2);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4a")]
        public async Task RestClient_WithExpiredToken_WhenTokenExpired_ShouldNotRetryAndRaiseError(Protocol protocol)
        {
            var helper = new RSA4Helper(this);

            // Get a very short lived token and wait for it to expire
            var authClient = await GetRestClient(protocol);
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(1) });
            await Task.Delay(TimeSpan.FromMilliseconds(2));

            // Modify the expiry date to fool the client it has a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            // create a new client with the token
            // set the Key to an empty string to override the sandbox settings
            var restClient = await helper.GetRestClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            // check the client thinks the token is valid
            restClient.AblyAuth.CurrentToken.IsValidToken().Should().BeTrue();
            try
            {
                await restClient.Channels.Get("random").PublishAsync("event", "data");
                throw new Exception("Unexpected success, the proceeding code should have raised an AblyException");
            }
            catch (AblyException e)
            {
                // the server responds with a token error
                // (401 HTTP status code and an Ably error value 40140 <= code < 40150)
                e.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                e.ErrorInfo.Code.Should().BeInRange(40140, 40150);
            }

            // did not retry the request
            helper.Requests.Count.Should().Be(1, "only one request should have been attempted");
            helper.Requests[0].Url.Should().Be("/channels/random/messages", "only the publish request should have been attempted");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4a")]
        public async Task RealtimeClient_NewInstanceWithExpiredToken_ShouldNotRetryAndHaveError(Protocol protocol)
        {
            var helper = new RSA4Helper(this);
            var authClient = await GetRestClient(protocol);
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(1) });
            await Task.Delay(TimeSpan.FromMilliseconds(2));

            // Modify the expiry date to fool the client it has a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            // get a realtime client with no key
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            bool connected = false;
            realtimeClient.Connection.Once(ConnectionState.Connected, (_) => { connected = true; });

            // assert that there is no pre-existing error
            realtimeClient.Connection.ErrorReason.Should().BeNull();

            await realtimeClient.WaitForState(ConnectionState.Failed);
            realtimeClient.Connection.State.Should().Be(ConnectionState.Failed);
            connected.Should().BeFalse();

            realtimeClient.Connection.ErrorReason.Code.Should().BeInRange(40140, 40150);
            helper.Requests.Count.Should().Be(0);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4a")]
        public async Task RealtimeClient_ConnectedWithExpiringToken_WhenTokenExpired_ShouldNotRetryAndHaveError(Protocol protocol)
        {
            var helper = new RSA4Helper(this);

            // Create a token that is valid long enough for a successful connection to occur
            var authClient = await GetRestClient(protocol);
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(2000) });

            // get a realtime client with no Key, AuthUrl, or authCallback
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            await realtimeClient.WaitForState(ConnectionState.Connected);

            // assert that there is no pre-existing error
            realtimeClient.Connection.ErrorReason.Should().BeNull();

            await realtimeClient.WaitForState(ConnectionState.Failed);
            realtimeClient.Connection.State.Should().Be(ConnectionState.Failed);

            realtimeClient.Connection.ErrorReason.Code.Should().BeInRange(40140, 40150);
            helper.Requests.Count.Should().Be(0);
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
        public async Task WithApiKey_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldRaiseError(Protocol protocol)
        {
            var helper = new RSA4Helper(this);

            var restClient = await GetRestClient(protocol);
            await restClient.Auth.AuthorizeAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000)
            });

            // when an HTTP request is made return a response with a 500 status, causing retry to fail
            restClient.ExecuteHttpRequest = helper.AblyResponseWith500Status;

            // this realtime client will have a key for the sandbox, thus a means to renew
            var realtimeClient = await GetRealtimeClient(protocol, (options, _) => { options.TokenDetails = restClient.Options.TokenDetails; }, options => restClient);

            realtimeClient.RestClient.ExecuteHttpRequest = helper.AblyResponseWith500Status;
            await realtimeClient.WaitForState(ConnectionState.Connected);
            var channel = realtimeClient.Channels.Get("random");

            // wait for the token to expire and then publish
            await Task.Delay(TimeSpan.FromMilliseconds(2000));
            try
            {
                channel.Publish("event", "data");
                throw new Exception("Unexpected success, channel.Publish() should have thrown an AblyException");
            }
            catch (Exception e)
            {
                (e is AblyException).Should().BeTrue("should be an Ably Exception");
            }

            helper.Requests.Count.Should().Be(1);
            helper.Requests[0].Url.EndsWith("requestToken").Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4b")]
        public async Task WithAuthCallback_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldRaiseError(Protocol protocol)
        {
            // create a short lived token
            var authRestClient = await GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000)
            });

            // create a realtime client with the token and provide an auth callback that will throw an exception
            var realtimeClient = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.AuthCallback = tokenParams => throw new Exception("AuthCallback failed");
            });

            await realtimeClient.WaitForState(ConnectionState.Connected);
            var channel = realtimeClient.Channels.Get("random");

            // wait for the token to expire and then try to publish
            await Task.Delay(TimeSpan.FromMilliseconds(2000));

            try
            {
                channel.Publish("event", "data");
                throw new Exception("Unexpected success, channel.Publish() should have thrown an AblyException");
            }
            catch (Exception e)
            {
                (e is AblyException).Should().BeTrue("should be an Ably Exception");
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4b")]
        public async Task WithAuthUrl_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldRaiseError(Protocol protocol)
        {
            var authRestClient = await GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000)
            });

            // this realtime client will have a key for the sandbox, thus a means to renew
            var realtimeClient = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.LogLevel = LogLevel.Debug;
                options.AuthUrl = new Uri(_invalidAuthUrl);
            });

            await realtimeClient.WaitForState(ConnectionState.Connected);
            var channel = realtimeClient.Channels.Get("random");

            // wait for the token to expire and then publish
            await Task.Delay(TimeSpan.FromMilliseconds(2000));
            try
            {
                channel.Publish("event", "data");
                throw new Exception("Unexpected success, channel.Publish() should have thrown an AblyException");
            }
            catch (Exception e)
            {
                (e is AblyException).Should().BeTrue("should be an AblyException");
            }
        }

        /*
           (RSA4c) If an attempt by the realtime client library to authenticate is made using the authUrl or authCallback,
           and the request to authUrl fails (unless RSA4d applies), the callback authCallback results in an error
           (unless RSA4d applies), an attempt to exchange a TokenRequest for a TokenDetails results in an error
           (unless RSA4d applies), the provided token is in an invalid format, or the attempt times out after realtimeRequestTimeout, then:

           (RSA4c1) An ErrorInfo with code 80019 and description of the underlying failure should be emitted
           with the state change, in the errorReason and/or in the callback as appropriate

           (RSA4c2) If the connection is CONNECTING, then the connection attempt should be treated as unsuccessful,
           and as such the connection should transition to the DISCONNECTED or SUSPENDED state as defined in RTN14 and RTN15

           (RSA4c3)If the connection is CONNECTED, then the connection should remain CONNECTED

         */

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c2")]
        public async Task AuthToken_WithConnectingRealtimeClient_WhenAuthUrlFails_ShouldRaiseError(Protocol protocol)
        {
            throw new Exception("WIP test stub");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c2")]
        public async Task AuthToken_WithConnectingRealtimeClient_WhenAuthCallbackFails_ShouldRaiseError(Protocol protocol)
        {
            throw new Exception("WIP test stub");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c3")]
        public async Task AuthToken_WithConnectedRealtimeClient_WhenAuthUrlFails_ShouldRaiseError(Protocol protocol)
        {
            throw new Exception("WIP test stub");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c3")]
        public async Task AuthToken_WithConnectedRealtimeClient_WhenAuthCallbackFails_ShouldRaiseError(Protocol protocol)
        {
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
            await ably.Auth.AuthorizeAsync(new TokenParams() { ClientId = "123" }, new AuthOptions());
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
                UseBinaryProtocol = protocol == Defaults.Protocol,
                HttpRequestTimeout = new TimeSpan(0, 0, 20)
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
                options.AuthCallback = async tokenParams =>
                {
                    var token = await tokenClient.Auth.CreateTokenRequestAsync(new TokenParams() { ClientId = "*" });
                    return new AuthCallbackResult(token);
                };
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
                options.AuthCallback = async tokenParams =>
                {
                    var token = await tokenClient.Auth.CreateTokenRequestAsync(new TokenParams() { ClientId = "*" });
                    return new AuthCallbackResult(token);
                };
                options.Environment = settings.Environment;
                options.UseBinaryProtocol = protocol == Defaults.Protocol;
            });

            var channel = authCallbackClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        /// <summary>
        /// Helper methods that return an AblyRest or AblyRealitme instance and a list of AblyRequest that
        /// will contain all the HTTP requests the client attempts
        /// </summary>
        private class RSA4Helper
        {
            private AuthSandboxSpecs Specs { get; set; }

            public List<AblyRequest> Requests { get; set; }

            public RSA4Helper(AuthSandboxSpecs specs)
            {
                Requests = new List<AblyRequest>();
                Specs = specs;
            }

            public async Task<AblyRest> GetRestClientWithRequests(Protocol protocol, TokenDetails token, bool invalidateKey, Action<ClientOptions> optionsAction = null)
            {
                void DefaultOptionsAction(ClientOptions options)
                {
                    options.TokenDetails = token;
                    if (invalidateKey)
                    {
                        options.Key = string.Empty;
                    }
                }

                if (optionsAction == null)
                {
                    optionsAction = DefaultOptionsAction;
                }

                var restClient = await Specs.GetRestClient(protocol, optionsAction);

                // intercept http calls to demostrate that the
                // client did not attempt to request a new token
                var execute = restClient.ExecuteHttpRequest;
                restClient.ExecuteHttpRequest = request =>
                {
                    Requests.Add(request);
                    return execute.Invoke(request);
                };

                return restClient;
            }

            public async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null)
            {
                var restClient = await Specs.GetRestClient(protocol, optionsAction);

                // intercept http calls to demostrate that the
                // client did not attempt to request a new token
                var execute = restClient.ExecuteHttpRequest;
                restClient.ExecuteHttpRequest = request =>
                {
                    Requests.Add(request);
                    return execute.Invoke(request);
                };

                return restClient;
            }

            public async Task<AblyRealtime> GetRealTimeClientWithRequests(Protocol protocol, TokenDetails token, bool invalidateKey, Action<ClientOptions, TestEnvironmentSettings> optionsAction = null)
            {
                var restClient = await GetRestClientWithRequests(protocol, token, invalidateKey);

                // Creating a new connection
                void DefaultOptionsAction(ClientOptions options, TestEnvironmentSettings settings)
                {
                    options.TokenDetails = token;
                    if (invalidateKey)
                    {
                        options.Key = string.Empty;
                    }
                }

                if (optionsAction == null)
                {
                    optionsAction = DefaultOptionsAction;
                }

                var realtimeClient = await Specs.GetRealtimeClient(protocol, optionsAction, options => restClient);
                return realtimeClient;
            }

            public async Task<AblyRealtime> GetRealtimeClient(Protocol protocol, Action<ClientOptions, TestEnvironmentSettings> optionsAction = null)
            {
                var client = await Specs.GetRealtimeClient(protocol, optionsAction);
                var execHttp = client.RestClient.ExecuteHttpRequest;
                client.RestClient.ExecuteHttpRequest = request =>
                {
                    Requests.Add(request);
                    return execHttp(request);
                };
                return client;
            }

            public Task<AblyResponse> AblyResponseWith401Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "text/plain", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.Unauthorized };
                return Task.FromResult(r);
            }

            public Task<AblyResponse> AblyResponseWith403Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "text/plain", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.Forbidden };
                throw AblyException.FromResponse(r);
            }

            public Task<AblyResponse> AblyResponseWith500Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "text/plain", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.InternalServerError };
                throw AblyException.FromResponse(r);
            }
        }
    }
}
