using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using Ably.Auth;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Ably.Tests
{
    public class AuthorisationTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);

        private Rest GetRestClient()
        {
            var rest = new Rest(ApiKey);
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return new AblyResponse() { TextResponse = "{}" };
            };

            Config.Now = () => Now;
            return rest;
        }

        private static string GetKeyId()
        {
            return ApiKey.Split(':')[0];
        }

        [Fact]
        public void TokenShouldNotBeSetBeforeAuthoriseIsCalled()
        {
            var client = GetRestClient();
            client.CurrentToken.Should().BeNull();
        }

        [Fact]
        public void RequestToken_CreatesPostRequestWithCorrectUrl()
        {
            //Arrange
            SendRequestTokenWithValidOptions();

            //Assert
            Assert.Equal("/keys/" + GetKeyId() + "/requestToken", CurrentRequest.Url);
            Assert.Equal(HttpMethod.Post, CurrentRequest.Method);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutId_UsesRestClientDefaultKeyId()
        {
            var request = new TokenRequest();

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(client.Options.KeyId, data.id);
        }

        [Fact]
        public void RequestToken_WithNoRequestAndNoExtraOptions_CreatesDefaultRequestWithIdClientIdAndBlankCapability()
        {
            var client = GetRestClient();
            client.Options.ClientId = "Test";
            client.Auth.RequestToken(null, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(GetKeyId(), data.id);
            Assert.Equal(Capability.AllowAll.ToJson(), data.capability);
            Assert.Equal(client.Options.ClientId, data.clientId);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutCapability_SetsBlankCapability()
        {
            var request = new TokenRequest() { Id = "123" };

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(Capability.AllowAll.ToJson(), data.capability);
        }

        [Fact]
        public void RequestToken_TimeStamp_SetsTimestampOnTheDataRequest()
        {
            var date = new DateTime(2014, 1, 1);
            var request = new TokenRequest() { Timestamp = date };

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(date.ToUnixTime().ToString(), data.timestamp);
        }

        [Fact]
        public void RequestToken_WithoutTimeStamp_SetsCurrentTimeOnTheRequest()
        {
            var request = new TokenRequest();

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(Now.ToUnixTime().ToString(), data.timestamp);
        }

        [Fact]
        public void RequestToken_SetsRequestTokenRequestToRequestPostData()
        {
            SendRequestTokenWithValidOptions();

            Assert.IsType<TokenRequestPostData>(CurrentRequest.PostData);
        }

        [Fact]
        public void RequestToken_WithExplicitKeyIdAndKeyValue_UsesCorrectKeyIdAndValueToCreateTheRequest()
        {
            var request = new TokenRequest();
            var options = new AuthOptions()
            {
                KeyId = "AAAAAA.BBBBBB",
                KeyValue = "keyvalue"
            };
            var client = GetRestClient();

            client.Auth.RequestToken(request, options);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(options.KeyId, data.id);
            var currentMac = data.mac;

            data.CalculateMac(options.KeyValue);
            Assert.Equal(data.mac, currentMac);
        }

        [Fact]
        public void RequestToken_WithQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var rest = GetRestClient();
            var currentTime = DateTimeOffset.UtcNow;
            rest.ExecuteHttpRequest = x =>
                {
                    if (x.Url.Contains("time"))
                        return new AblyResponse { TextResponse = "[" + currentTime.ToUnixTimeInMilliseconds() + "]", Type = ResponseType.Json };

                    //Assert
                    var data = x.PostData as TokenRequestPostData;
                    Assert.Equal(data.timestamp, currentTime.ToUnixTime().ToString());
                    return new AblyResponse() { TextResponse = "{}" };
                };
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), Id = GetKeyId() };

            //Act
            rest.Auth.RequestToken(request, new AuthOptions() { QueryTime = true });
        }

        [Fact]
        public void RequestToken_WithoutQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var rest = GetRestClient();
            rest.ExecuteHttpRequest = x =>
            {
                Assert.False(x.Url.Contains("time"));
                return new AblyResponse() { TextResponse = "{}" };
            };

            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), Id = GetKeyId() };

            //Act
            rest.Auth.RequestToken(request, new AuthOptions() { QueryTime = false });
        }

        [Fact]
        public void RequestToken_WithRequestCallback_RetrievesTokenFromCallback()
        {
            var rest = GetRestClient();
            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            var authCallbackCalled = false;
            var token = new Token();
            var options = new AuthOptions
            {
                AuthCallback = (x) =>
                {
                    authCallbackCalled = true;
                    return token;
                }
            };
            var result = rest.Auth.RequestToken(tokenRequest, options);

            Assert.True(authCallbackCalled);
            Assert.Same(token, result);
        }

        [Fact]
        public void RequestToken_WithAuthUrlAndDefaultAuthMethod_SendsGetRequestToTheUrl()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl",
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
            };

            AblyRequest authRequest = null;
            var requestdata = new TokenRequestPostData { id = GetKeyId(), capability = "123" };
            rest.ExecuteHttpRequest = x =>
            {
                if (x.Url == options.AuthUrl)
                {
                    authRequest = x;
                    return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                }
                return new AblyResponse { TextResponse = "{}" };
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            //Act
            rest.Auth.RequestToken(tokenRequest, options);

            //Assert
            Assert.Equal(HttpMethod.Get, authRequest.Method);
            Assert.Equal(options.AuthHeaders, authRequest.Headers);
            Assert.Equal(options.AuthParams, authRequest.QueryParameters);
        }

        [Fact]
        public void RequestToken_WithAuthUrl_SendPostRequestToAuthUrl()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl",
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
                AuthMethod = HttpMethod.Post
            };
            AblyRequest authRequest = null;
            var requestdata = new TokenRequestPostData { id = GetKeyId(), capability = "123" };
            rest.ExecuteHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl)
                {
                    authRequest = x;
                    return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                }
                return new AblyResponse { TextResponse = "{}" };
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(HttpMethod.Post, authRequest.Method);

            Assert.Equal(options.AuthHeaders, authRequest.Headers);
            Assert.Equal(options.AuthParams, authRequest.PostParameters);
            Assert.Equal(options.AuthUrl, authRequest.Url);
        }

        [Fact]
        public void RequestToken_WithAuthUrlWhenTokenIsReturned_ReturnsToken()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };

            var dateTime = DateTime.UtcNow;
            rest.ExecuteHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl)
                {
                    return new AblyResponse
                    {
                        TextResponse = "{ " +
                                       "\"id\":\"123\"," +
                                       "\"expires\":" + dateTime.ToUnixTime() + "," +
                                       "\"issued_at\":" + dateTime.ToUnixTime() + "," +
                                       "\"capability\":\"{}\"," +
                                       "\"clientId\":\"111\"" +
                                       "}"
                    };
                }
                return new AblyResponse { TextResponse = "{}" };
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            var token = rest.Auth.RequestToken(tokenRequest, options);
            Assert.NotNull(token);
            dateTime.Should().BeWithin(TimeSpan.FromSeconds(1)).After(token.IssuedAt);
        }

        [Fact]
        public void RequestToken_WithAuthUrl_GetsResultAndPostToRetrieveToken()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };
            List<AblyRequest> requests = new List<AblyRequest>();
            var requestdata = new TokenRequestPostData { id = GetKeyId(), capability = "123" };
            rest.ExecuteHttpRequest = (x) =>
            {
                requests.Add(x);
                if (x.Url == options.AuthUrl)
                {
                    return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                }
                return new AblyResponse { TextResponse = "{}" };
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(2, requests.Count);
            Assert.Equal(requestdata, requests.Last().PostData);
        }

        [Fact]
        public void RequestToken_WithAuthUrlWhichReturnsAnErrorThrowsAblyException()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };

            rest.ExecuteHttpRequest = (x) => rest._httpClient.Execute(x);

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            Assert.Throws<AblyException>(delegate { rest.Auth.RequestToken(tokenRequest, options); });
        }

        [Fact]
        public void RequestToken_WithAuthUrlWhichReturnsNonJsonContentType_ThrowsException()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };
            rest.ExecuteHttpRequest = (x) => new AblyResponse { Type = ResponseType.Other };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            Assert.Throws<AblyException>(delegate { rest.Auth.RequestToken(tokenRequest, options); });
        }

        [Fact]
        public void Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new Token() { ExpiresAt = Config.Now().AddHours(1) };

            var token = client.Auth.Authorise(null, null, false);

            Assert.Same(client.CurrentToken, token);
        }

        [Fact]
        public void Authorise_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetRestClient();
            client.Auth.Authorise(new TokenRequest() {Ttl = TimeSpan.FromMinutes(260)}, null, false);

            client.Auth.Authorise(null, null, false);
            var data = CurrentRequest.PostData as TokenRequestPostData;
            data.ttl.Should().Be(TimeSpan.FromMinutes(260).TotalSeconds.ToString());
        }

        [Fact]
        public void Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new Token() { ExpiresAt = Config.Now().AddHours(1) };

            var token = client.Auth.Authorise(new TokenRequest() { ClientId = "123", Capability = new Capability(), Id = "123" }, null, true);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        [Fact]
        public void Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new Token() { ExpiresAt = Config.Now().AddHours(-1) };

            var token = client.Auth.Authorise(null, null, false);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        private TokenRequest SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), Id = GetKeyId() };

            //Act
            rest.Auth.RequestToken(request, null);
            return request;
        }
    }

    public class AuthCreateTokenRequestAcceptanceTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);
        public Rest Client { get; set; }

        public AuthCreateTokenRequestAcceptanceTests()
        {
            Client = GetRestClient();
        }

        private Rest GetRestClient()
        {
            var rest = new Rest(ApiKey);
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return new AblyResponse() {TextResponse = "{}"};
            };

            Config.Now = () => Now;
            return rest;
        }

        [Fact]
        public void UsesKeyIdFromTheClient()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.id.Should().Be(Client.Options.KeyId);
        }

        [Fact]
        public void UsesDefaultTtlWhenNoneIsSpecified()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.ttl.Should().Be(TokenRequest.Defaults.Ttl.TotalSeconds.ToString());
        }

        [Fact]
        public void UsesTheDefaultCapability()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.capability.Should().Be(TokenRequest.Defaults.Capability.ToJson());
        }

        [Fact]
        public void UsesUniqueNonseWhichIsMoreThan16Characters()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            var secondTime = Client.Auth.CreateTokenRequest(null, null);
            data.nonce.Should().NotBe(secondTime.nonce);
            data.nonce.Length.Should().BeGreaterOrEqualTo(16);
        }

        [Fact]
        public void WithCapabilityOverridesDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();

            var data = Client.Auth.CreateTokenRequest(new TokenRequest() {Capability = capability}, null);
            data.capability.Should().Be(capability.ToJson());
        }

        [Fact]
        public void WithTtlOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() {Ttl = TimeSpan.FromHours(2)}, null);

            data.ttl.Should().Be(TimeSpan.FromHours(2).TotalSeconds.ToString());
        }

        [Fact]
        public void WithNonceOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() { Nonce = "Blah" }, null);
            data.nonce.Should().Be("Blah");
        }

        [Fact]
        public void WithTimeStampOverridesDefault()
        {
            var date = new DateTime(2014, 1, 1);
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() { Timestamp= date }, null);
            data.timestamp.Should().Be(date.ToUnixTime().ToString());
        }

        [Fact]
        public void WithClientIdOverridesDefault()
        {
            var data = Client.Auth.CreateTokenRequest(new TokenRequest() { ClientId = "123"}, null);
            data.clientId.Should().Be("123");
        }

        [Fact]
        public void WithQueryTimeQueriesForTimestamp()
        {
            var currentTime = Config.Now();
            Client.ExecuteHttpRequest = x => 
                new AblyResponse { TextResponse = "[" + currentTime.ToUnixTimeInMilliseconds() + "]", Type = ResponseType.Json };
            var data = Client.Auth.CreateTokenRequest(null, new AuthOptions() {QueryTime = true});
            data.timestamp.Should().Be(currentTime.ToUnixTime().ToString());
        }

        [Fact]
        public void WithOutKeyIdThrowsException()
        {
            var client = new Rest(new AblyOptions() { AppId = "123"});
            Assert.Throws<AblyException>(delegate { client.Auth.CreateTokenRequest(null, null); });
        }

        [Fact]
        public void WithOutKeyValueThrowsException()
        {
            var client = new Rest(new AblyOptions() { AppId = "123", KeyId = "222"});
            Assert.Throws<AblyException>(delegate { client.Auth.CreateTokenRequest(null, null); });
        }

        [Fact]
        public void GeneratesHMac()
        {
            var data = Client.Auth.CreateTokenRequest(null, null);
            data.mac.Should().NotBeEmpty();
        }
    }

    public class ImplicitTokenAuthWithClientId
    {
        private Mock<IAblyHttpClient> _ablyHttpClient;
        private string _clientId;
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public Rest Client { get; set; }

        public ImplicitTokenAuthWithClientId()
        {
            _clientId = "123";
            Client = new Rest(new AblyOptions() {Key = ApiKey, ClientId = _clientId});
            _ablyHttpClient = new Mock<IAblyHttpClient>();
            Client._httpClient = _ablyHttpClient.Object;
            _ablyHttpClient.Setup(x => x.Execute(It.IsAny<AblyRequest>())).Returns((AblyRequest request) =>
            {
                if (request.Url.Contains("requestToken"))
                {
                    return new AblyResponse() { TextResponse = "{ \"access_token\": { \"id\": \"unique-token-id\"}}" };
                }
                return new AblyResponse() { TextResponse = "{}" };
            }).Verifiable();
        }

        [Fact]
        public void WhenPublishing_WillSendATokenRequestToServer()
        {
            Client.Channels.Get("test").Publish("test", true);

            _ablyHttpClient.Verify(x => x.Execute(It.IsAny<AblyRequest>()), Times.Exactly(2));
        }

        [Fact]
        public void BeforeSendingAMessage_CurrentTokenIsNull()
        {
            Client.CurrentToken.Should().BeNull();
        }

        [Fact]
        public void AfterSendingAMessage_CurrentTokenHasDefaultCapabilityAndTtl()
        {
            Client.Channels.Get("test").Publish("test", true);

            Client.CurrentToken.Should().NotBeNull();
        }
    }


    public class AuthRequestTokenAcceptanceTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);

        private Rest GetRestClient()
        {
            var rest = new Rest(ApiKey);
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return new AblyResponse() {TextResponse = "{}"};
            };

            Config.Now = () => Now;
            return rest;
        }

        private void RequestToken(TokenRequest request, AuthOptions authOptions,
            Action<TokenRequestPostData, AblyRequest> action)
        {
            var rest = GetRestClient();

            rest.ExecuteHttpRequest = x =>
            {
                //Assert
                var data = x.PostData as TokenRequestPostData;
                action(data, x);
                return new AblyResponse() {TextResponse = "{}"};
            };

            rest.Auth.RequestToken(request, authOptions);
        }

        [Fact]
        public void WithOverridingClientId_OverridesTheDefault()
        {
            var tokenRequest = new TokenRequest {ClientId = "123"};
            RequestToken(tokenRequest, null, (data, request) => Assert.Equal("123", data.clientId));
        }

        [Fact]
        public void WithOverridingCapability_OverridesTheDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();
            var tokenRequest = new TokenRequest {Capability = capability};

            RequestToken(tokenRequest, null, (data, request) => Assert.Equal(capability.ToJson(), data.capability));
        }

        [Fact]
        public void WithOverridingNonce_OverridesTheDefault()
        {
            RequestToken(new TokenRequest {Nonce = "Blah"}, null, (data, request) => Assert.Equal("Blah", data.nonce));
        }

        [Fact]
        public void WithOverridingTimeStamp_OverridesTheDefault()
        {
            var timeStamp = new DateTime(2015, 1, 1);
            var tokenRequest = new TokenRequest {Timestamp = timeStamp};
            RequestToken(tokenRequest, null,
                (data, request) => Assert.Equal(timeStamp.ToUnixTime().ToString(), data.timestamp));
        }

        [Fact]
        public void WithOverridingTtl_OverridesTheDefault()
        {
            RequestToken(new TokenRequest {Ttl = TimeSpan.FromSeconds(2)}, null,
                (data, request) => Assert.Equal(TimeSpan.FromSeconds(2).TotalSeconds.ToString(), data.ttl));
        }

        [Fact]
        public void WithKeyIdAndKeySecret_PassesKeyIdAndUsesKeySecretToSignTheRequest()
        {
            var keyId = "Blah";
            var keyValue = "BBB";

            RequestToken(new TokenRequest(), new AuthOptions() {KeyId = keyId, KeyValue = keyValue}, (data, request) =>
            {
                Assert.Contains(keyId, request.Url);
                var values = new[]
                {
                    data.id,
                    data.ttl,
                    data.capability,
                    data.clientId,
                    data.timestamp,
                    data.nonce
                };

                var signText = string.Join("\n", values) + "\n";
                var expectedResult = signText.ComputeHMacSha256(keyValue);
                Assert.Equal(expectedResult, data.mac);
            });
        }
    }



}
