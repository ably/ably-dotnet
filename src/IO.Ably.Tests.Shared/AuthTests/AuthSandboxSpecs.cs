using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
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

            var channelName = "RSA4a".AddRandomSuffix();

            try
            {
                await restClient.Channels.Get(channelName).PublishAsync("event", "data");
                throw new Exception("Unexpected success, the preceding code should have raised an AblyException");
            }
            catch (AblyException e)
            {
                // the server responds with a token error
                // (401 HTTP status code and an Ably error value 40140 <= code < 40150)
                // As the token is expired we can expect a specific code "40142": "token expired"
                e.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                e.ErrorInfo.Code.Should().Be(40142);
            }

            // did not retry the request
            helper.Requests.Count.Should().Be(1, "only one request should have been attempted");
            helper.Requests[0].Url.Should().Be($"/channels/{channelName}/messages", "only the publish request should have been attempted");
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

            realtimeClient.Connection.ErrorReason.Code.Should().Be(40142);
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
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(8000) });

            // get a realtime client with no Key, AuthUrl, or authCallback
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            await realtimeClient.WaitForState(ConnectionState.Connected);

            // assert that there is no pre-existing error
            realtimeClient.Connection.ErrorReason.Should().BeNull();

            await realtimeClient.WaitForState(ConnectionState.Failed);
            realtimeClient.Connection.State.Should().Be(ConnectionState.Failed);

            realtimeClient.Connection.ErrorReason.Code.Should().Be(40142);
            helper.Requests.Count.Should().Be(0);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4b")]
        public async Task RealtimeWithAuthError_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError(Protocol protocol)
        {
            var helper = new RSA4Helper(this);

            var restClient = await GetRestClient(protocol);
            var token = await restClient.Auth.AuthorizeAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000)
            });

            // this realtime client will have a key for the sandbox, thus a means to renew
            var realtimeClient = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.AutoConnect = false;
            });

            realtimeClient.RestClient.ExecuteHttpRequest = helper.AblyResponseWith500Status;

            var awaiter = new TaskCompletionAwaiter(5000);

            realtimeClient.Connection.Once(ConnectionState.Disconnected, state =>
            {
                state.Reason.Code.Should().Be(80019);
                awaiter.SetCompleted();
            });

            await Task.Delay(2000);
            realtimeClient.Connect();

            var result = await awaiter.Task;
            result.Should().BeTrue();
            helper.Requests.Count.Should().Be(1);
            helper.Requests[0].Url.EndsWith("requestToken").Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4b")]
        public async Task RealTimeWithAuthCallback_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError(Protocol protocol)
        {
            // create a short lived token
            var authRestClient = await GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000)
            });

            bool didRetry = false;
            var realtimeClient = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.AuthCallback = tokenParams =>
                {
                    didRetry = true;
                    throw new Exception("AuthCallback failed");
                };
                options.AutoConnect = false;
            });

            var awaiter = new TaskCompletionAwaiter(5000);
            realtimeClient.Connection.Once(ConnectionState.Disconnected, state =>
            {
                state.Reason.Code.Should().Be(80019);
                awaiter.SetCompleted();
            });

            await Task.Delay(2000);
            realtimeClient.Connect();

            var result = await awaiter.Task;
            result.Should().BeTrue();
            didRetry.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4b")]
        public async Task RealTimeWithAuthUrl_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError(Protocol protocol)
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
                options.AutoConnect = false;
            });

            var awaiter = new TaskCompletionAwaiter(5000);
            realtimeClient.Connection.Once(ConnectionState.Disconnected, state =>
            {
                state.Reason.Code.Should().Be(80019);
                awaiter.SetCompleted();
            });

            await Task.Delay(2000);
            realtimeClient.Connect();

            var result = await awaiter.Task;
            result.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4c")]
        [Trait("spec", "RSA4c1")]
        [Trait("spec", "RSA4c2")]
        [Trait("spec", "RSA4c3")]
        public async Task Auth_WithRealtimeClient_WhenAuthFails_ShouldTransitionToOrRemainInTheCorrectState(Protocol protocol)
        {
            async Task TestConnectingBecomesDisconnected(string context, Action<ClientOptions, TestEnvironmentSettings> optionsAction)
            {
                TaskCompletionAwaiter tca = new TaskCompletionAwaiter(5000);
                var realtimeClient = await GetRealtimeClient(protocol, optionsAction);
                realtimeClient.Connection.On(ConnectionState.Disconnected, change =>
                {
                    change.Previous.Should().Be(ConnectionState.Connecting);
                    change.Reason.Code.Should().Be(80019);
                    tca.SetCompleted();
                });

                realtimeClient.Connection.Connect();
                (await tca.Task).Should().BeTrue(context);
            }

            // authUrl fails
            void AuthUrlOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthUrl = new Uri(_invalidAuthUrl);
                options.RealtimeRequestTimeout = TimeSpan.FromSeconds(2);
                options.HttpRequestTimeout = TimeSpan.FromSeconds(2);
            }

            // authCallback fails
            void AuthCallbackOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthCallback = (tokenParams) => throw new Exception("AuthCallback force error");
            }

            // invalid token returned
            void InvalidTokenOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthCallback = (tokenParams) => Task.FromResult<object>("invalid:token");
            }

            await TestConnectingBecomesDisconnected("With invalid AuthUrl connection becomes Disconnected", AuthUrlOptions);
            await TestConnectingBecomesDisconnected("With invalid AuthCallback Connection becomes Disconnected", AuthCallbackOptions);
            await TestConnectingBecomesDisconnected("With Invalid Token Connection becomes Disconnected", InvalidTokenOptions);

            /* RSA4c3 */

            async Task<TokenDetails> GetToken()
            {
                var authRestClient = await GetRestClient(protocol);
                var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
                {
                    Ttl = TimeSpan.FromMilliseconds(2000)
                });
                return token;
            }

            async Task TestConnectedStaysConnected(string context, Action<ClientOptions, TestEnvironmentSettings> optionsAction)
            {
                var token = await GetToken();
                token.Expires = DateTimeOffset.Now.AddMinutes(30);
                void Options(ClientOptions options, TestEnvironmentSettings settings)
                {
                    optionsAction(options, settings);
                    options.TokenDetails = token;
                }

                TaskCompletionAwaiter tca = new TaskCompletionAwaiter(1000);
                var realtimeClient = await GetRealtimeClient(protocol, Options);

                realtimeClient.Connect();
                await realtimeClient.WaitForState(ConnectionState.Connected);
                realtimeClient.Connection.On(change =>
                {
                    // this callback should not be called
                    change.Previous.Should().Be(ConnectionState.Connected);
                    change.Reason.Code.Should().Be(80019);
                    tca.SetCompleted();
                });
                await realtimeClient.Auth.AuthorizeAsync();
                realtimeClient.Connection.State.Should().Be(ConnectionState.Connected);
                (await tca.Task).Should().BeFalse(context);
            }

            await TestConnectedStaysConnected("With invalid AuthUrl Connection remains Connected", AuthUrlOptions);
            await TestConnectedStaysConnected("With invalid AuthCallback connection remains Connected", AuthCallbackOptions);
            await TestConnectedStaysConnected("With Invalid Token connection remains Connected", InvalidTokenOptions);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSA4d")]
        public async Task Auth_WithRealtimeClient_WhenAuthFailsWith403_ShouldTransitionToFailed(Protocol protocol)
        {
            async Task Test403BecomesFailed(string context, Action<ClientOptions, TestEnvironmentSettings> optionsAction)
            {
                TaskCompletionAwaiter tca = new TaskCompletionAwaiter(5000);
                var realtimeClient = await GetRealtimeClient(protocol, optionsAction);

                realtimeClient.Connection.On(ConnectionState.Failed, change =>
                {
                    change.Previous.Should().Be(ConnectionState.Connecting);
                    change.Reason.Code.Should().Be(80019);
                    tca.SetCompleted();
                });

                realtimeClient.Connection.Connect();
                (await tca.Task).Should().BeTrue(context);
            }

            // authUrl fails
            void AuthUrlOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthUrl = new Uri("https://echo.ably.io/respondwith?status=403");
            }

            await Test403BecomesFailed("With 403 response should become Failed", AuthUrlOptions);
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
                var r = new AblyResponse(string.Empty, "application/json", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.Unauthorized };
                throw AblyException.FromResponse(r);
                return Task.FromResult(r);
            }

            public Task<AblyResponse> AblyResponseWith403Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "application/json", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.Forbidden };
                throw AblyException.FromResponse(r);
                return Task.FromResult(r);
            }

            public Task<AblyResponse> AblyResponseWith500Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "application/json", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.InternalServerError };
                return Task.FromResult(r);
            }
        }
    }
}
