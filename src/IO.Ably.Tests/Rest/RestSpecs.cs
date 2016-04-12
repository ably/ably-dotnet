using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.AcceptanceTests;
using IO.Ably.Auth;
using Xunit;

namespace IO.Ably.Tests
{
    public class RestSpecs : MockHttpSpecs
    {
        [Trait("spec", "RSC1")]
        public class WhenInitialisingRestClient
        {
            [Fact]
            public void WithInvalidKey_ThrowsAnException()
            {
                Assert.Throws<AblyException>(() => new AblyRest("InvalidKey"));
            }

            [Fact]
            public void WithValidKey_InitialisesTheClient()
            {
                var client = new AblyRest(ValidKey);
                Assert.NotNull(client);
            }

            [Fact]
            public void WithKeyInOptions_InitialisesTheClient()
            {
                var client = new AblyRest(new ClientOptions(ValidKey));
                Assert.NotNull(client);
            }

            [Fact]
            public void Ctor_WithKeyPassedInOptions_InitializesClient()
            {
                var client = new AblyRest(opts => opts.Key = ValidKey);
                Assert.NotNull(client);
            }

            [Fact]
            public void WithTokenAndClientId_InitializesClient()
            {
                var client = new AblyRest(opts =>
                {
                    opts.Token = "blah";
                    opts.ClientId = "123";
                });

                Assert.Equal(AuthMethod.Token, client.AblyAuth.AuthMethod);
            }
        }

        [Fact]
        [Trait("spec", "RSC2")]
        public void DefaultLoggerSinkShouldbeSetup()
        {
            Logger.LoggerSink.Should().BeOfType<DefaultLoggerSink>();
        }

        [Fact]
        [Trait("spec", "RSC3")]
        public void DefaultLogLevelShouldBeWarning()
        {
            Logger.LogLevel.Should().Be(LogLevel.Warning);
        }

        [Fact]
        [Trait("spec", "RSC4")]
        public void ACustomLoggerCanBeProvided()
        {
            var sink = new TestLoggerSink();
            Logger.LoggerSink = sink;
            Logger.Error("Boo");

            sink.LastLevel.Should().Be(LogLevel.Error);
            sink.LastMessage.Should().Be("Boo");
        }

        [Fact]
        [Trait("spec", "RSC5")]
        public void RestClientProvidesAccessToAuthObjectInstantiatedWithSameOptionsPassedToRestConstructor()
        {
            //Arrange
            var options = new ClientOptions(ValidKey);

            //Act
            var client = new AblyRest(options);

            //Assert
            var auth = client.Auth as AblyAuth;
            auth.Options.Should().BeSameAs(options);
        }

        [Theory]
        [Trait("spec", "RSC7")]
        [Trait("spec", "RSC18")]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldInitialiseAblyHttpClientWithCorrectTlsValue(bool tls)
        {
            var client = new AblyRest(new ClientOptions(ValidKey) { Tls = tls });
            client.HttpClient.IsSecure.Should().Be(tls);
        }

        [Fact]
        [Trait("spec", "RSC8a")]

        public void ShouldUseBinaryProtocolByDefault()
        {
            var client = new AblyRest(ValidKey);
            client.Options.UseBinaryProtocol.Should().BeTrue();
            client.Protocol.Should().Be(Protocol.MsgPack);
        }

        [Fact]
        [Trait("spec", "RSC8b")]
        public void WhenBinaryProtoclIsFalse_ShouldSetProtocolToJson()
        {
            var client = GetRestClient(setOptionsAction: opts => opts.UseBinaryProtocol = false);
            client.Protocol.Should().Be(Protocol.Json);
        }

        public class WithInvalidToken : MockHttpSpecs
        {
            [Theory]
            [InlineData(Defaults.TokenErrorCodesRangeStart)]
            [InlineData(Defaults.TokenErrorCodesRangeStart + 1)]
            [InlineData(Defaults.TokenErrorCodesRangeEnd)]
            [Trait("spec", "RSC10")]
            public async Task WhenErrorCodeIsTokenSpecific_ShouldAutomaticallyTryToRenewTokenIfRequestFails(int errorCode)
            {
                var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };
                bool firstAttempt = true;
                var client = GetConfiguredRestClient(errorCode, tokenDetails);

                await client.Stats();

                client.Auth.CurrentToken.Expires.Should().BeCloseTo(Now.AddDays(1));
                client.Auth.CurrentToken.ClientId.Should().Be("123");
            }

            [Fact]
            public async Task WhenErrorCodeIsNotTokenSpecific_ShouldThrow()
            {
                var client = GetConfiguredRestClient(40100, null);

                Assert.ThrowsAsync<AblyException>(() => client.Stats());
            }

            private AblyRest GetConfiguredRestClient(int errorCode, TokenDetails tokenDetails)
            {
                bool firstAttempt = true;
                var client = GetRestClient(request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        return new TokenDetails("123") { Expires = Now.AddDays(1), ClientId = "123" }.ToJson().ToAblyResponse();
                    }

                    if (firstAttempt)
                    {
                        firstAttempt = false;
                        throw new AblyException(new ErrorInfo("", errorCode));
                    }
                    return AblyResponse.EmptyResponse.ToTask();
                }, opts => { opts.TokenDetails = tokenDetails; });
                return client;
            }
        }

        [Trait("spec", "RSC11")]
        public class HostSpecs : AblySpecs
        {
            private FakeHttpMessageHandler _handler;
            public HostSpecs()
            {
                var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("12345678") };
                _handler = new FakeHttpMessageHandler(response);
            }

            private AblyRest CreateClient(Action<ClientOptions> optionsClient)
            {
                var options = new ClientOptions(ValidKey);
                optionsClient(options);
                var client = new AblyRest(options);
                client.HttpClient.CreateInternalHttpClient(_handler);
                return client;
            }

            [Fact]
            public async Task WithoutHostSpecific_ShouldUseDefaultHost()
            {
                var client = CreateClient(options => { });
                await MakeAnyRequest(client);
                _handler.LastRequest.RequestUri.Host.Should().Be(Defaults.RestHost);
            }

            [Fact]
            public async Task WithHostSpecifiedInOption_ShouldUseCustomHost()
            {
                var client = CreateClient(options => { options.RestHost = "www.test.com"; });
                await MakeAnyRequest(client);
                _handler.LastRequest.RequestUri.Host.Should().Be("www.test.com");
            }

            [Theory]
            [InlineData(AblyEnvironment.Sandbox)]
            [InlineData(AblyEnvironment.Uat)]
            public async Task WithEnvironmentAndNoCustomHost_ShouldPrefixEnvironment(AblyEnvironment environment)
            {
                var client = CreateClient(options => { options.Environment = environment; });
                await MakeAnyRequest(client);
                var expected = environment.ToString().ToLower() + "-" + Defaults.RestHost;
                _handler.LastRequest.RequestUri.Host.Should().Be(expected);
            }

            [Fact]
            public async Task WithEnvironmentAndCustomHost_ShouldUseCustomHostAsIs()
            {
                var client = CreateClient(options =>
                {
                    options.Environment = AblyEnvironment.Sandbox;
                    options.RestHost = "www.test.com";
                });
                await MakeAnyRequest(client);
                _handler.LastRequest.RequestUri.Host.Should().Be("www.test.com");
            }


            private static async Task MakeAnyRequest(AblyRest client)
            {
                await client.Channels.Get("boo").Publish("boo", "baa");
            }
        }

        [Fact]
        public async Task Init_WithCallback_ExecutesCallbackOnFirstRequest()
        {
            bool called = false;
            var options = new ClientOptions
            {
                AuthCallback = (x) => { called = true; return Task.FromResult(new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddHours(1) }); },
                UseBinaryProtocol = false
            };

            var rest = new AblyRest(options);

            rest.ExecuteHttpRequest = delegate { return "[{}]".ToAblyResponse(); };

            await rest.Stats();

            Assert.True(called, "Rest with Callback needs to request token using callback");
        }

        [Fact]
        public async Task Init_WithAuthUrl_CallsTheUrlOnFirstRequest()
        {
            bool called = false;
            var options = new ClientOptions
            {
                AuthUrl = "http://testUrl",
                UseBinaryProtocol = false
            };

            var rest = new AblyRest(options);

            rest.ExecuteHttpRequest = request =>
            {
                if (request.Url.Contains(options.AuthUrl))
                {
                    called = true;
                    return "{}".ToAblyResponse();
                }

                if (request.Url.Contains("requestToken"))
                {
                    return ("{ \"access_token\": { \"expires\": \"" + DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeInMilliseconds() + "\"}}").ToAblyResponse();
                }

                return "[{}]".ToAblyResponse();
            };

            await rest.Stats();

            Assert.True(called, "Rest with Callback needs to request token using callback");
        }

        [Fact]
        public async Task ClientWithExpiredTokenAutomaticallyCreatesANewOne()
        {
            Config.Now = () => DateTimeOffset.UtcNow;
            bool newTokenRequested = false;
            var options = new ClientOptions
            {
                AuthCallback = (x) =>
                {
                    newTokenRequested = true;
                    return Task.FromResult(new TokenDetails("new.token")
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(1)
                    });
                },
                UseBinaryProtocol = false
            };
            var rest = new AblyRest(options);
            rest.ExecuteHttpRequest = request => "[{}]".ToAblyResponse();
            rest.Auth.CurrentToken = new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddDays(-2) };

            await rest.Stats();
            newTokenRequested.Should().BeTrue();
            rest.Auth.CurrentToken.Token.Should().Be("new.token");
        }

        [Fact]
        public async Task ClientWithExistingTokenReusesItForMakingRequests()
        {
            var options = new ClientOptions
            {
                ClientId = "test",
                Key = "best",
                UseBinaryProtocol = false
            };
            var rest = new AblyRest(options);
            var token = new TokenDetails("123") { Expires = DateTimeOffset.UtcNow.AddHours(1) };
            rest.Auth.CurrentToken = token;

            rest.ExecuteHttpRequest = request =>
            {
                //Assert
                request.Headers["Authorization"].Should().Contain(token.Token.ToBase64());
                return "[{}]".ToAblyResponse();
            };

            await rest.Stats();
            await rest.Stats();
            await rest.Stats();
        }

        [Fact]
        public void Init_WithTokenId_SetsTokenRenewableToFalse()
        {
            var rest = new AblyRest(new ClientOptions() { Token = "token_id" });

            rest.AblyAuth.TokenRenewable.Should().BeFalse();
        }

        [Fact]
        public void AddAuthHeader_WithBasicAuthentication_AddsCorrectAuthorisationHeader()
        {
            //Arrange
            var rest = new AblyRest(ValidKey);
            ApiKey key = ApiKey.Parse(ValidKey);
            var request = new AblyRequest("/test", HttpMethod.Get, Protocol.Json);
            var expectedValue = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(key.ToString()));

            //Act
            rest.AblyAuth.AddAuthHeader(request).Wait();

            //Assert
            var authHeader = request.Headers.First();
            Assert.Equal("Authorization", authHeader.Key);

            Assert.Equal(expectedValue, authHeader.Value);
        }

        [Fact]
        public void ChannelsGet_ReturnsNewChannelWithName()
        {
            var rest = GetRestClient();

            var channel = rest.Channels.Get("Test");

            Assert.Equal("Test", channel.Name);
        }
    }
}
