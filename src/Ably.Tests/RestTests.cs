using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ably.Auth;
using FluentAssertions;
using Xunit;

namespace Ably.Tests
{
    public class RestTests
    {
        private const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";
        private readonly ApiKey Key = ApiKey.Parse(ValidKey);

        class FakeHttpClient : IAblyHttpClient
        {
            public Func<AblyRequest, AblyResponse> ExecuteFunc = delegate { return new AblyResponse(); };
            public AblyResponse Execute(AblyRequest request)
            {
                return ExecuteFunc(request);
            }
        }

        private static RestClient GetRestClient()
        {
            return new RestClient(new AblyOptions() { UseBinaryProtocol = false, Key = ValidKey });
        }

        //[Fact]
        //public void Ctor_WithNoParametersAndAblyConnectionString_RetrievesApiKeyFromConnectionString()
        //{
        //    Assert.DoesNotThrow(delegate
        //    {
        //        var rest = new RestClient();

        //        Assert.NotNull(rest);
        //    });
        //}

        [Fact]
        public void Ctor_WithNoParametersWithInvalidKey_ThrowsInvalidKeyException()
        {
            Assert.Throws<AblyException>(delegate
            {
                new RestClient("InvalidKey");
            });
        }

        [Fact]
        public void Ctor_WithKeyPassedInOptions_InitialisesClient()
        {
            var client = new RestClient(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithKeyInOptions_InitialisesClient()
        {
            var client = new RestClient(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithKeyAndNoClientId_SetsAuthMethodToBasic()
        {
            var client = new RestClient(ValidKey);
            Assert.Equal(AuthMethod.Basic, client.AuthMethod);
        }

        [Fact]
        public void Init_WithKeyAndClientId_SetsAuthMethodToToken()
        {
            var client = new RestClient(new AblyOptions { Key = ValidKey, ClientId = "123" });
            Assert.Equal(AuthMethod.Token, client.AuthMethod);
        }

        [Fact]
        public void Init_WithKeyNoClientIdAndAuthTokenId_SetsCurrentTokenWithSuppliedId()
        {
            AblyOptions options = new AblyOptions { Key = ValidKey, ClientId = "123", Token = "222" };
            var client = new RestClient(options);

            Assert.Equal(options.Token, client.CurrentToken.Token);
        }

        [Fact]
        public void Init_WithouthKey_SetsAuthMethodToToken()
        {
            var client = new RestClient(opts =>
            {
                opts.Token = "blah";
                opts.ClientId = "123";
            });

            Assert.Equal(AuthMethod.Token, client.AuthMethod);
        }

        [Fact]
        public void Init_WithCallback_ExecutesCallbackOnFirstRequest()
        {
            bool called = false;
            var options = new AblyOptions
            {
                AuthCallback = (x) => { called = true; return new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddHours(1) }; },
                UseBinaryProtocol = false
            };

            var rest = new RestClient(options);

            rest.ExecuteHttpRequest = delegate { return new AblyResponse() { TextResponse = "[{}]" }; };

            rest.Stats();

            Assert.True(called, "Rest with Callback needs to request token using callback");
        }

        [Fact]
        public void Init_WithAuthUrl_CallsTheUrlOnFirstRequest()
        {
            bool called = false;
            var options = new AblyOptions
            {
                AuthUrl = "http://testUrl",
                UseBinaryProtocol = false
            };

            var rest = new RestClient(options);

            rest.ExecuteHttpRequest = request =>
            {
                if (request.Url.Contains(options.AuthUrl))
                {
                    called = true;
                    return new AblyResponse() { TextResponse = "{}" };
                }

                if (request.Url.Contains("requestToken"))
                {
                    return new AblyResponse { TextResponse = "{ \"access_token\": { \"expires\": \"" + DateTimeOffset.UtcNow.AddHours(1).ToUnixTime() + "\"}}" };
                }

                return new AblyResponse() { TextResponse = "[{}]" };
            };

            rest.Stats();

            Assert.True(called, "Rest with Callback needs to request token using callback");
        }

        [Fact]
        public void ClientWithExpiredTokenAutomaticallyCreatesANewOne()
        {
            Config.Now = () => DateTimeOffset.UtcNow;
            var newTokenRequested = false;
            var options = new AblyOptions
            {
                AuthCallback = (x) => { 
                    
                    Console.WriteLine("Getting new token.");
                    newTokenRequested = true; return new TokenDetails("new.token")
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(1)
                }; },
                UseBinaryProtocol = false
            };
            var rest = new RestClient(options);
            rest.ExecuteHttpRequest = request =>
            {
                Console.WriteLine("Getting an AblyResponse.");
                return new AblyResponse() {TextResponse = "[{}]"};
            };
            rest.CurrentToken = new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddDays(-2) };

            Console.WriteLine("Current time:" + Config.Now());
            rest.Stats();
            newTokenRequested.Should().BeTrue();
            rest.CurrentToken.Token.Should().Be("new.token");
        }

        [Fact]
        public void ClientWithExistingTokenReusesItForMakingRequests()
        {
            var options = new AblyOptions
            {
                ClientId = "test",
                Key = "best",
                UseBinaryProtocol = false
            };
            var rest = new RestClient(options);
            var token = new TokenDetails("123") { Expires = DateTimeOffset.UtcNow.AddHours(1) };
            rest.CurrentToken = token;

            rest.ExecuteHttpRequest = request =>
            {
                //Assert
                request.Headers["Authorization"].Should().Contain(token.Token.ToBase64());
                return new AblyResponse() { TextResponse = "[{}]" };
            };

            rest.Stats();
            rest.Stats();
            rest.Stats();
        }

        [Fact]
        public void Init_WithTokenId_SetsTokenRenewableToFalse()
        {
            var rest = new RestClient(new AblyOptions() { Token = "token_id" });

            rest.TokenRenewable.Should().BeFalse();
        }

        [Fact]
        public void AddAuthHeader_WithBasicAuthentication_AddsCorrectAuthorisationHeader()
        {
            //Arrange
            var rest = new RestClient(ValidKey);
            ApiKey key = ApiKey.Parse(ValidKey);
            var request = new AblyRequest("/test", "GET", Protocol.Json);
            var expectedValue = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(key.ToString()));

            //Act
            rest.AddAuthHeader(request);

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
            rest.ExecuteHttpRequest = x => { request = x; return new AblyResponse { Type = ResponseType.Json, TextResponse = "[{  }]" }; };
            rest.Stats();

            Assert.Equal("GET", request.Method);
            Assert.Equal("/stats", request.Url);
        }


        [Fact]
        public void Stats_WithQuery_SetsCorrectRequestHeaders()
        {
            var rest = GetRestClient();
            AblyRequest request = null;
            rest.ExecuteHttpRequest = x => { request = x; return new AblyResponse { TextResponse = "[{}]" }; };
            var query = new StatsDataRequestQuery();
            DateTime now = DateTime.Now;
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
                return response;
            };

            //Act
            var result = rest.Stats();

            //Assert
            Assert.NotNull(result.NextQuery);
            Assert.NotNull(result.FirstQuery);
        }
    }
}
