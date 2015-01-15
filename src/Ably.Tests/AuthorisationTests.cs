using Ably.Auth;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
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
            rest.ExecuteRequest = (request) =>
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
            Assert.Equal("", data.capability);
            Assert.Equal(client.Options.ClientId, data.client_id);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutCapability_SetsBlankCapability()
        {
            var request = new TokenRequest() { Id = "123" };

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal("", data.capability);
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
            var currentTime = DateTime.Now.ToUniversalTime();
            rest.ExecuteRequest = x =>
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
            rest.ExecuteRequest = x =>
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
            rest.ExecuteRequest = x =>
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
            rest.ExecuteRequest = (x) =>
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

            var dateTime = DateTime.Now;
            rest.ExecuteRequest = (x) =>
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
                                       "\"client_id\":\"111\"" +
                                       "}"
                    };
                }
                return new AblyResponse { TextResponse = "{}" };
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };

            var token = rest.Auth.RequestToken(tokenRequest, options);
            Assert.NotNull(token);
            Assert.Equal(dateTime, token.IssuedAt);
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
            rest.ExecuteRequest = (x) =>
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

            rest.ExecuteRequest = (x) => rest._client.Execute(x);

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
            rest.ExecuteRequest = (x) => new AblyResponse { Type = ResponseType.Other };

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
        public void Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new Token() { ExpiresAt = Config.Now().AddHours(1) };

            var token = client.Auth.Authorise(new TokenRequest() { ClientId = "123", Capability = new Capability(), Id = "123" }, null, true);

            Assert.Contains("requestToken", CurrentRequest.Url);
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

    public class AuthRequestTokenAcceptanceTests
    {
        private const string ApiKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);

        private Rest GetRestClient()
        {
            var rest = new Rest(ApiKey);
            rest.ExecuteRequest = (request) =>
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

        private void RequestToken(TokenRequest request, AuthOptions authOptions,
            Action<TokenRequestPostData, AblyRequest> action)
        {
            var rest = GetRestClient();

            rest.ExecuteRequest = x =>
            {
                //Assert
                var data = x.PostData as TokenRequestPostData;
                action(data, x);
                return new AblyResponse() { TextResponse = "{}" };
            };

            rest.Auth.RequestToken(request, authOptions);
        }

        [Fact]
        public void WithOverridingClientId_OverridesTheDefault()
        {
            var tokenRequest = new TokenRequest { ClientId = "123" };
            RequestToken(tokenRequest, null, (data, request) => Assert.Equal("123", data.client_id));
        }

        [Fact]
        public void WithOverridingCapability_OverridesTheDefault()
        {
            var capability = new Capability();
            capability.AddResource("test").AllowAll();
            var tokenRequest = new TokenRequest { Capability = capability };

            RequestToken(tokenRequest, null, (data, request) => Assert.Equal(capability.ToJson(), data.capability));
        }

        [Fact]
        public void WithOverridingNonce_OverridesTheDefault()
        {
            RequestToken(new TokenRequest { Nonce = "Blah" }, null, (data, request) => Assert.Equal("Blah", data.nonce));
        }

        [Fact]
        public void WithOverridingTimeStamp_OverridesTheDefault()
        {
            var timeStamp = new DateTime(2015, 1, 1);
            var tokenRequest = new TokenRequest { Timestamp = timeStamp };
            RequestToken(tokenRequest, null, (data, request) => Assert.Equal(timeStamp.ToUnixTime().ToString(), data.timestamp));
        }

        [Fact]
        public void WithOverridingTtl_OverridesTheDefault()
        {
            RequestToken(new TokenRequest { Ttl = TimeSpan.FromSeconds(2) }, null,
                (data, request) => Assert.Equal(TimeSpan.FromSeconds(2).TotalSeconds.ToString(), data.ttl));
        }

        [Fact]
        public void WithKeyIdAndKeySecret_PassesKeyIdAndUsesKeySecretToSignTheRequest()
        {
            var keyId = "Blah";
            var keyValue = "BBB";

            RequestToken(new TokenRequest(), new AuthOptions() { KeyId = keyId, KeyValue = keyValue }, (data, request) =>
            {
                Assert.Contains(keyId, request.Url);
                var testData = new TokenRequestPostData();
                var values = new[] 
                { 
                    data.id, 
                    data.ttl,
                    data.capability, 
                    data.client_id, 
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
