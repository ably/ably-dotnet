using Ably.Auth;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            return ApiKey.Split(':')[0].Split('.')[1];
        }

        [Fact]
        public void RequestToken_CreatesPostRequestWithCorrectUrl()
        {
            //Arrange
            SendRequestTokenWithValidOptions();

            //Assert
            Assert.Equal("/apps/AHSz6w/authorise", CurrentRequest.Url);
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
        public void RequestToken_SetsRequestTokenRequestToRequestPostData()
        {
            SendRequestTokenWithValidOptions();

            Assert.IsType<TokenRequestPostData>(CurrentRequest.PostData);
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
        public void RequestToken_WithRequestCallback_RetrievesTokenDataFromCallback()
        {   
            var rest = GetRestClient();
            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };
            var requestdata = new TokenRequestPostData { id = GetKeyId(), capability = "123" };

            var authCallbackCalled = false;
            var options = new AuthOptions
            {
                AuthCallback = (x) => { authCallbackCalled = true; return JsonConvert.SerializeObject(requestdata); }
            };
            rest.Auth.RequestToken(tokenRequest, options);

            Assert.True(authCallbackCalled);
            Assert.Equal(requestdata, CurrentRequest.PostData);
        }

        [Fact]
        public void RequestToken_WithAuthUrl_SendsPostRequestToThePostUrlFollowedByRequestTokenRequest()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://testUrl",
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } }
            };
            List<AblyRequest> requests = new List<AblyRequest>();
            var requestdata = new TokenRequestPostData { id = GetKeyId(), capability = "123" };
            rest.ExecuteRequest = (x) => { 
                requests.Add(x);
                if (x.Url == options.AuthUrl)
                    {
                        return new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) };
                    } 
                else
                    return new AblyResponse { TextResponse = "{}" } ; 
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };
            
            rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(2, requests.Count);
            Assert.Equal(options.AuthHeaders, requests.First().Headers);
            Assert.Equal(options.AuthParams, requests.First().PostParameters);
            Assert.Equal(options.AuthUrl, requests.First().Url);
            Assert.Equal(requestdata, requests.Last().PostData);
        }

        [Fact]
        public void Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new Token() { Expires = Config.Now().AddHours(1) };

            var token = client.Auth.Authorise(null, null, false);

            Assert.Same(client.CurrentToken, token);
        }

        [Fact]
        public void Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new Token() { Expires = Config.Now().AddHours(1) };

            var token = client.Auth.Authorise(new TokenRequest() { ClientId = "123", Capability = new Capability(), Id = "123"}, null, true);

            Assert.Contains("authorise", CurrentRequest.Url);
        }

        private TokenRequest SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), Id =GetKeyId() };

            //Act
            rest.Auth.RequestToken(request, null);
            return request;
        }
    }

}
