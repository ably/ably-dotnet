using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Ably.Auth;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Ably.Tests
{
    public class AuthorisationTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTimeOffset Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc).ToDateTimeOffset();
        private readonly string _dummyTokenResponse = "{ \"access_token\": {}}";

        private RestClient GetRestClient()
        {
            var rest = new RestClient(new AblyOptions() { Key = ApiKey, UseBinaryProtocol = false});
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return new AblyResponse() { TextResponse = _dummyTokenResponse };
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
            Assert.Equal(client.Options.ParseKey().KeyId, data.keyName);
        }

        [Fact]
        public void RequestToken_WithNoRequestAndNoExtraOptions_CreatesDefaultRequestWithIdClientIdAndBlankCapability()
        {
            var client = GetRestClient();
            client.Options.ClientId = "Test";
            client.Auth.RequestToken(null, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(GetKeyId(), data.keyName);
            Assert.Equal(Capability.AllowAll.ToJson(), data.capability);
            Assert.Equal(client.Options.ParseKey().KeyId, data.clientId);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutCapability_SetsBlankCapability()
        {
            var request = new TokenRequest() { KeyName = "123" };

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(Capability.AllowAll.ToJson(), data.capability);
        }

        [Fact]
        public void RequestToken_TimeStamp_SetsTimestampOnTheDataRequest()
        {
            var date = new DateTime(2014, 1, 1).ToDateTimeOffset();
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

        //[Fact]
        //public void RequestToken_WithExplicitKeyIdAndKeyValue_UsesCorrectKeyIdAndValueToCreateTheRequest()
        //{
        //    var request = new TokenRequest();
        //    var options = new AuthOptions()
        //    {
        //        KeyId = "AAAAAA.BBBBBB",
        //        KeyValue = "keyvalue"
        //    };
        //    var client = GetRestClient();

        //    client.Auth.RequestToken(request, options);

        //    var data = CurrentRequest.PostData as TokenRequestPostData;
        //    Assert.Equal(options.KeyId, data.keyName);
        //    var currentMac = data.mac;

        //    data.CalculateMac(options.KeyValue);
        //    Assert.Equal(data.mac, currentMac);
        //}

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
                    return new AblyResponse() { TextResponse = _dummyTokenResponse };
                };
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), KeyName = GetKeyId() };

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
                return new AblyResponse() { TextResponse = _dummyTokenResponse };
            };

            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), KeyName = GetKeyId() };

            //Act
            rest.Auth.RequestToken(request, new AuthOptions() { QueryTime = false });
        }

        [Fact]
        public void RequestToken_WithRequestCallback_RetrievesTokenFromCallback()
        {
            var rest = GetRestClient();
            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            var authCallbackCalled = false;
            var token = new TokenDetails();
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
            var requestdata = new TokenRequestPostData { keyName = GetKeyId(), capability = "123" };
            rest.ExecuteHttpRequest = x =>
            {
                if (x.Url == options.AuthUrl)
                {
                    authRequest = x;
                    return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                }
                return new AblyResponse { TextResponse = _dummyTokenResponse };
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

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
            var requestdata = new TokenRequestPostData { keyName = GetKeyId(), capability = "123" };
            rest.ExecuteHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl)
                {
                    authRequest = x;
                    return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                }
                return new AblyResponse { TextResponse = _dummyTokenResponse };
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

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

            var dateTime = DateTimeOffset.UtcNow;
            rest.ExecuteHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl)
                {
                    return new AblyResponse
                    {
                        TextResponse = "{ " +
                                       "\"keyName\":\"123\"," +
                                       "\"expires\":" + dateTime.ToUnixTime() + "," +
                                       "\"issued\":" + dateTime.ToUnixTime() + "," +
                                       "\"capability\":\"{}\"," +
                                       "\"clientId\":\"111\"" +
                                       "}"
                    };
                }
                return new AblyResponse { TextResponse = "{}" };
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

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
            var requestdata = new TokenRequestPostData { keyName = GetKeyId(), capability = "123" };
            rest.ExecuteHttpRequest = (x) =>
            {
                requests.Add(x);
                if (x.Url == options.AuthUrl)
                {
                    return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                }
                return new AblyResponse { TextResponse = _dummyTokenResponse };
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

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

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

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
            rest.ExecuteHttpRequest = (x) => new AblyResponse { Type = ResponseType.Binary };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            Assert.Throws<AblyException>(delegate { rest.Auth.RequestToken(tokenRequest, options); });
        }

        [Fact]
        public void Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

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
            client.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = client.Auth.Authorise(new TokenRequest() { ClientId = "123", Capability = new Capability(), KeyName = "123" }, null, true);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        [Fact]
        public void Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(-1) };

            var token = client.Auth.Authorise(null, null, false);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        private TokenRequest SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), KeyName = GetKeyId() };

            //Act
            rest.Auth.RequestToken(request, null);
            return request;
        }
    }
}
