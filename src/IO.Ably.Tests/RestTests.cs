using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.AcceptanceTests;
using IO.Ably.Auth;
using Xunit;

namespace IO.Ably.Tests
{
    public class RestTests
    {
        private const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";
        
        private static AblyRest GetRestClient()
        {
            return new AblyRest(new ClientOptions() { UseBinaryProtocol = false, Key = ValidKey });
        }

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

                Assert.Equal(AuthMethod.Token, client.AuthMethod);
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
            var options = new ClientOptions(ValidKey);
            var client = new AblyRest(options);

            var auth = client.Auth as AblyTokenAuth;
            auth.Options.Should().BeSameAs(options);
        }


        [Fact]
        public void Init_WithKeyAndNoClientId_SetsAuthMethodToBasic()
        {
            var client = new AblyRest(ValidKey);
            Assert.Equal(AuthMethod.Basic, client.AuthMethod);
        }

        [Fact]
        public void Init_WithKeyAndClientId_SetsAuthMethodToToken()
        {
            var client = new AblyRest(new ClientOptions { Key = ValidKey, ClientId = "123" });
            Assert.Equal(AuthMethod.Token, client.AuthMethod);
        }

        [Fact]
        public void Init_WithKeyNoClientIdAndAuthTokenId_SetsCurrentTokenWithSuppliedId()
        {
            ClientOptions options = new ClientOptions { Key = ValidKey, ClientId = "123", Token = "222" };
            var client = new AblyRest(options);

            Assert.Equal(options.Token, client.CurrentToken.Token);
        }

        [Fact]
        public void Init_WithouthKey_SetsAuthMethodToToken()
        {
            var client = new AblyRest(opts =>
            {
                opts.Token = "blah";
                opts.ClientId = "123";
            });

            Assert.Equal(AuthMethod.Token, client.AuthMethod);
        }

        [Fact]
        public async Task Init_WithCallback_ExecutesCallbackOnFirstRequest()
        {
            bool called = false;
            var options = new ClientOptions
            {
                AuthCallback = (x) => { called = true; return new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddHours(1) }; },
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
                    return new TokenDetails("new.token")
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(1)
                    };
                },
                UseBinaryProtocol = false
            };
            var rest = new AblyRest(options);
            rest.ExecuteHttpRequest = request => "[{}]".ToAblyResponse();
            rest.CurrentToken = new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddDays(-2) };

            await rest.Stats();
            newTokenRequested.Should().BeTrue();
            rest.CurrentToken.Token.Should().Be("new.token");
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
            rest.CurrentToken = token;

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

            rest.TokenRenewable.Should().BeFalse();
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
            rest.AddAuthHeader(request).Wait();

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

        [Fact]
        public void Stats_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();

            AblyRequest request = null;
            rest.ExecuteHttpRequest = x => { request = x; return "[{  }]".ToAblyJsonResponse(); };
            rest.Stats();

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/stats", request.Url);
        }


        [Fact]
        public void Stats_WithQuery_SetsCorrectRequestHeaders()
        {
            var rest = GetRestClient();
            AblyRequest request = null;
            rest.ExecuteHttpRequest = x => { request = x; return "[{}]".ToAblyResponse(); };
            var query = new StatsDataRequestQuery();
            var now = DateTimeOffset.UtcNow;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            rest.Stats(query);

            request.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            request.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            request.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            request.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }

        [Fact]
        public void Stats_ReturnsCorrectFirstAndNextLinks()
        {
            //Arrange
            var rest = GetRestClient();

            rest.ExecuteHttpRequest = request =>
            {
                var response = new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleStatsRequestHeaders(),
                    TextResponse = "[{}]"
                };
                return response.ToTask();
            };

            //Act
            var result = rest.Stats().Result;

            //Assert
            Assert.NotNull(result.NextQuery);
            Assert.NotNull(result.FirstQuery);
        }
    }
}
