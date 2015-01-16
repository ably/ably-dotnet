using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Ably.Auth;
using FluentAssertions;
using Xunit;

namespace Ably.Tests
{

    public class RestProtocolTests
    {
        [Fact]
        public void WhenProtocolIsNotDefined_DefaultsToMsgPack()
        {
            var rest = new Rest(new AblyOptions());
            rest.Protocol.Should().Be(Protocol.MsgPack);
        }

        [Fact]
        public void WhenProtocolIsJson_RestProtocolIsSetToJson()
        {
            var rest = new Rest(new AblyOptions() {Protocol = Protocol.Json});
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenUseBinaryIsFalse_ProtocolIsSetToJson()
        {
            var rest = new Rest(new AblyOptions() {UseBinaryProtocol = false});
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenProtocolIsMsgPack_ProtocolIsSetToMsgPack()
        {
            var rest = new Rest(new AblyOptions() { Protocol = Protocol.MsgPack});
            rest.Protocol.Should().Be(Protocol.MsgPack);
        }
    }

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

        private class RestThatReadsDummyConnectionString : Rest
        {
            internal override string GetConnectionString()
            {
                return "";
            }
        }

        private static Rest GetRestClient()
        {
            return new Rest(ValidKey);
        }

        [Fact]
        public void Ctor_WithNoParametersAndNoAblyConnectionString_Throws()
        {
            Assert.Throws<AblyException>(delegate {
             new RestThatReadsDummyConnectionString();
            });
        }

        [Fact]
        public void Ctor_WithNoParametersAndAblyConnectionString_RetrievesApiKeyFromConnectionString()
        {
            var rest = new Rest();

            Assert.NotNull(rest);
        }

        [Fact]
        public void Ctor_WithNoParametersWithInvalidKey_ThrowsInvalidKeyException()
        {
            Assert.Throws<AblyException>(delegate
            {
                new Rest("InvalidKey");
            });
        }

        [Fact]
        public void Ctor_WithKeyPassedInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithKeyInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithAppIdInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.AppId = Key.AppId);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithKeyAndNoClientId_SetsAuthMethodToBasic()
        {
            var client = new Rest(ValidKey);
            Assert.Equal(AuthMethod.Basic, client.AuthMethod);
        }

        [Fact]
        public void Init_WithKeyAndClientId_SetsAuthMethodToToken()
        {
            var client = new Rest(new AblyOptions { Key = ValidKey, ClientId = "123" });
            Assert.Equal(AuthMethod.Token, client.AuthMethod);
        }

        [Fact]
        public void Init_WithKeyNoClientIdAndAuthTokenId_SetsCurrentTokenWithSuppliedId()
        {
            AblyOptions options = new AblyOptions { Key = ValidKey, ClientId = "123", AuthToken = "222" };
            var client = new Rest(options);

            Assert.Equal(options.AuthToken, client.CurrentToken.Id);
        }

        [Fact]
        public void Init_WithouthKey_SetsAuthMethodToToken()
        {
            var client = new Rest(opts =>
            {
                opts.KeyValue = "blah";
                opts.ClientId = "123";
                opts.AppId = "123";
            });

            Assert.Equal(AuthMethod.Token, client.AuthMethod);
        }

        [Fact]
        public void Init_WithCallback_ExecutesCallbackOnFirstRequest()
        {
            bool called = false;
            var options = new AblyOptions
            {
                AuthCallback = (x) => { called = true; return new Token(); },
                AppId = "-NyOAA" //Random
            };

            var rest = new Rest(options);

            var httpClient = new FakeHttpClient();
            httpClient.ExecuteFunc = delegate { return new AblyResponse() {TextResponse = "{}"}; };
            rest._httpClient = httpClient;

            rest.Stats();

            Assert.True(called, "Rest with Callback needs to request token using callback");
        }

        [Fact]
        public void Init_WithTokenId_SetsTokenRenewableToFalse()
        {
            var rest = new Rest(new AblyOptions() {AuthToken = "token_id"});

            rest.TokenRenewable.Should().BeFalse();
        }

        [Fact]
        public void AddAuthHeader_WithBasicAuthentication_AddsCorrectAuthorisationHeader()
        {
            //Arrange
            var rest = new Rest(ValidKey);
            ApiKey key = ApiKey.Parse(ValidKey);
            var request = new AblyRequest("/test", HttpMethod.Get, Protocol.Json);
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
            rest.ExecuteRequest = x => { request = x; return new AblyResponse { Type = ResponseType.Json, TextResponse = "{  }" };  };
            rest.Stats();

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/stats", request.Url);
        }

        
        [Fact]
        public void Stats_WithQuery_SetsCorrectRequestHeaders()
        {
            var rest = GetRestClient();
            AblyRequest request = null;
            rest.ExecuteRequest = x => { request = x; return new AblyResponse { TextResponse = "{}" }; };
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

            rest.ExecuteRequest = request =>
            {
                var response = new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleStatsRequestHeaders(),
                    TextResponse = "{}"
                };
                return response;
            };

            //Act
            var result = rest.Stats();

            //Assert
            Assert.NotNull(result.NextQuery);
            Assert.NotNull(result.InitialResultQuery);
        }
    }
}
