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
        private const string ApiKey = "AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob";
        public AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);

        private Rest GetRestClient()
        {
            var rest = new Rest(ApiKey);
            rest.ExecuteRequest = (request) =>
            {
                CurrentRequest = request;
                return null;
            };

            Config.Now = () => Now;
            return rest;
        }

        private static string GetKeyId()
        {
            return ApiKey.Split(':')[1];
        }

        private static string GetKeyValue()
        {
            return ApiKey.Split(':')[2];
        }


        [Fact]
        public void RequestToken_CreatesPostRequestWithCorrectUrl()
        {
            //Arrange
            SendRequestTokenWithValidOptions();

            //Assert
            Assert.Equal("/apps/AHSz6w/requestToken", CurrentRequest.Path);
            Assert.Equal(HttpMethod.Post, CurrentRequest.Method);
        }

        [Fact]
        public void RequestToken_SetsRequestTokenRequestToRequestPostData()
        {
            SendRequestTokenWithValidOptions();

            Assert.IsType<TokenRequestPostData>(CurrentRequest.PostData);
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
                if (x.Path == options.AuthUrl)
                    {
                        return new AblyResponse { Result = JsonConvert.SerializeObject(requestdata) };
                    } 
                else
                    return null; 
            };

            var tokenRequest = new TokenRequest { Id = GetKeyId(), Capability = new Capability() };
            
            rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(2, requests.Count);
            Assert.Equal(options.AuthHeaders, requests.First().Headers);
            Assert.Equal(options.AuthParams, requests.First().PostParameters);
            Assert.Equal(options.AuthUrl, requests.First().Path);
            Assert.Equal(requestdata, requests.Last().PostData);
        }

        private TokenRequest SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            rest.Auth.RequestToken(request, null);
            return request;
        }
    }

}
