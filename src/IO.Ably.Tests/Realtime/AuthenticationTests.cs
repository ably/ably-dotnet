using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Auth;
using Xunit;

namespace IO.Ably.Tests
{
    //TODO: Enable Tests after fixing all rest tests first
    class AuthenticationTests
    {
        private const string ApiKey = "123.456:789";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTimeOffset Now = DateHelper.CreateDate(2012, 12, 12, 10, 10, 10);
        private readonly string _dummyTokenResponse = "{ \"access_token\": {}}";

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
        public void RequestToken_WithTokenRequestWithoutId_UsesClientDefaultKeyId()
        {
            var client = GetClient();

            client.Auth.RequestToken(new TokenParams(), null);

            var data = CurrentRequest.PostData as TokenRequest;
            Assert.Equal(client.Options.ParseKey().KeyName, data.KeyName);
        }

        [Fact]
        public async Task RequestToken_WithNoRequestAndNoExtraOptions_CreatesDefaultRequestWithIdClientIdAndBlankCapability()
        {
            var client = GetClient();
            client.Options.ClientId = "Test";
            await client.Auth.RequestToken(null, null);

            var data = CurrentRequest.PostData as TokenRequest;
            Assert.Equal(GetKeyId(), data.KeyName);
            Assert.Equal(Capability.AllowAll.ToJson(), data.Capability);
            Assert.Equal(client.Options.ParseKey().KeyName, data.ClientId);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutCapability_SetsBlankCapability()
        {
            var client = GetClient();

            client.Auth.RequestToken(new TokenParams(), null);

            var data = CurrentRequest.PostData as TokenRequest;
            Assert.Equal(Capability.AllowAll.ToJson(), data.Capability);
        }

        [Fact]
        public void RequestToken_TimeStamp_SetsTimestampOnTheDataRequest()
        {
            var date = DateHelper.CreateDate(2014, 1, 1);
            var tokenParams = new TokenParams() { Timestamp = date };

            var client = GetClient();

            client.Auth.RequestToken(tokenParams, null);

            var data = CurrentRequest.PostData as TokenRequest;
            Assert.Equal(date.ToUnixTimeInMilliseconds().ToString(), data.Timestamp);
        }

        [Fact]
        public void RequestToken_WithoutTimeStamp_SetsCurrentTimeOnTheRequest()
        {
            var client = GetClient();

            client.Auth.RequestToken(new TokenParams(), null);

            var data = CurrentRequest.PostData as TokenRequest;
            Assert.Equal(Now.ToUnixTimeInMilliseconds().ToString(), data.Timestamp);
        }

        [Fact]
        public void RequestToken_SetsRequestTokenRequestToRequestPostData()
        {
            SendRequestTokenWithValidOptions();

            Assert.IsType<TokenRequest>(CurrentRequest.PostData);
        }

        [Fact]
        public void RequestToken_WithQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var currentTime = DateTimeOffset.UtcNow;
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = x =>
            {
                if (x.Url.Contains("time"))
                    return Task.FromResult(new AblyResponse { TextResponse = "[" + currentTime.ToUnixTimeInMilliseconds() + "]", Type = ResponseType.Json });

                //Assert
                var tokenRequest = x.PostData as TokenRequest;
                Assert.Equal(tokenRequest.Timestamp, currentTime.ToUnixTimeInMilliseconds().ToString());
                return Task.FromResult(new AblyResponse() { TextResponse = _dummyTokenResponse });
            };
            var rest = GetClient(executeHttpRequest);
            var tokenParams = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            rest.Auth.RequestToken(tokenParams, new AuthOptions() { QueryTime = true });
        }

        [Fact]
        public void RequestToken_WithoutQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = x =>
            {
                Assert.False(x.Url.Contains("time"));
                return Task.FromResult(new AblyResponse() { TextResponse = _dummyTokenResponse });
            };
            var rest = GetClient(executeHttpRequest);

            var tokenParams = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            rest.Auth.RequestToken(tokenParams, new AuthOptions() { QueryTime = false });
        }

        [Fact]
        public async Task RequestToken_WithRequestCallback_RetrievesTokenFromCallback()
        {
            var rest = GetClient();
            var tokenParams = new TokenParams { Capability = new Capability() };

            var authCallbackCalled = false;
            var token = new TokenDetails();
            var options = new AuthOptions
            {
                AuthCallback = (x) =>
                {
                    authCallbackCalled = true;
                    return Task.FromResult(token);
                }
            };
            var result = await rest.Auth.RequestToken(tokenParams, options);

            Assert.True(authCallbackCalled);
            Assert.Same(token, result);
        }

        [Fact]
        public void RequestToken_WithAuthUrlAndDefaultAuthMethod_SendsGetRequestToTheUrl()
        {
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl",
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
            };

            AblyRequest authRequest = null;
            var requestdata = new TokenRequest { KeyName = GetKeyId(), Capability = "123" };
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = x =>
            {
                if (x.Url == options.AuthUrl)
                {
                    authRequest = x;
                    return Task.FromResult(new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) });
                }
                return Task.FromResult(new AblyResponse { TextResponse = _dummyTokenResponse });
            };
            var rest = GetClient(executeHttpRequest);

            var tokenRequest = new TokenParams { Capability = new Capability() };

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
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl",
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
                AuthMethod = HttpMethod.Post
            };
            AblyRequest authRequest = null;
            var requestdata = new TokenRequest { KeyName = GetKeyId(), Capability = "123" };
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl)
                {
                    authRequest = x;
                    return Task.FromResult(new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) });
                }
                return Task.FromResult(new AblyResponse { TextResponse = _dummyTokenResponse });
            };
            var rest = GetClient(executeHttpRequest);

            var tokenParams = new TokenParams { Capability = new Capability() };

            rest.Auth.RequestToken(tokenParams, options);

            Assert.Equal(HttpMethod.Post, authRequest.Method);

            Assert.Equal(options.AuthHeaders, authRequest.Headers);
            Assert.Equal(options.AuthParams, authRequest.PostParameters);
            Assert.Equal(options.AuthUrl, authRequest.Url);
        }

        [Fact]
        public void RequestToken_WithAuthUrlWhenTokenIsReturned_ReturnsToken()
        {
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };

            var dateTime = DateTimeOffset.UtcNow;
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl)
                {
                    return Task.FromResult(new AblyResponse
                    {
                        TextResponse = "{ " +
                                       "\"keyName\":\"123\"," +
                                       "\"expires\":" + dateTime.ToUnixTimeInMilliseconds() + "," +
                                       "\"issued\":" + dateTime.ToUnixTimeInMilliseconds() + "," +
                                       "\"capability\":\"{}\"," +
                                       "\"clientId\":\"111\"" +
                                       "}"
                    });
                }
                return Task.FromResult(new AblyResponse { TextResponse = "{}" });
            };
            var rest = GetClient(executeHttpRequest);

            var tokenParams = new TokenParams { Capability = new Capability() };

            var token = rest.Auth.RequestToken(tokenParams, options).Result;
            Assert.NotNull(token);
            dateTime.Should().BeWithin(TimeSpan.FromSeconds(1)).After(token.Issued);
        }

        [Fact]
        public void RequestToken_WithAuthUrl_GetsResultAndPostToRetrieveToken()
        {
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };
            List<AblyRequest> requests = new List<AblyRequest>();
            var requestdata = new TokenRequest { KeyName = GetKeyId(), Capability = "123" };
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = (x) =>
            {
                requests.Add(x);
                if (x.Url == options.AuthUrl)
                {
                    return Task.FromResult(new AblyResponse { TextResponse = JsonConvert.SerializeObject(requestdata) });
                }
                return Task.FromResult(new AblyResponse { TextResponse = _dummyTokenResponse });
            };
            var rest = GetClient(executeHttpRequest);

            var tokenParams = new TokenParams { Capability = new Capability() };

            rest.Auth.RequestToken(tokenParams, options);

            Assert.Equal(2, requests.Count);
            Assert.Equal(requestdata, requests.Last().PostData);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhichReturnsAnErrorThrowsAblyException()
        {
            var rest = GetNotModifiedClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };

            var tokenParams = new TokenParams {  Capability = new Capability() };

            await Assert.ThrowsAsync<AblyException>(() => rest.Auth.RequestToken(tokenParams, options));
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhichReturnsNonJsonContentType_ThrowsException()
        {
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = (x) => Task.FromResult(new AblyResponse { Type = ResponseType.Binary });
            var rest = GetClient(executeHttpRequest);

            var tokenParams = new TokenParams { Capability = new Capability() };

            await Assert.ThrowsAsync<AblyException>(() => rest.Auth.RequestToken(tokenParams, options));
        }

        [Fact]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Same(client.Auth.CurrentToken, token);
        }

        [Fact]
        public async void Authorise_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetClient();
            await client.Auth.Authorise(new TokenParams() { Ttl = TimeSpan.FromMinutes(260) }, null, false);

            await client.Auth.Authorise(null, null, false);
            var data = CurrentRequest.PostData as TokenRequest;
            data.Ttl.Should().Be(TimeSpan.FromMinutes(260).TotalMilliseconds.ToString());
        }

        [Fact]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(new TokenParams() { ClientId = "123", Capability = new Capability() }, null, true);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        [Fact]
        public async Task Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(-1) };

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        private void SendRequestTokenWithValidOptions()
        {
            var rest = GetClient();
            var request = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10)};

            //Act
            rest.Auth.RequestToken(request, null);
        }

        private AblyRealtime GetClient(Func<AblyRequest, Task<AblyResponse>> executeHttpRequest)
        {
            var options = new ClientOptions() { Key = ApiKey, UseBinaryProtocol = false };
            var client = new AblyRealtime(options);
            client.RestClient.ExecuteHttpRequest = executeHttpRequest;

            client.InitAuth().Wait();

            Config.Now = () => Now;
            return client;
        }

        private AblyRealtime GetNotModifiedClient()
        {
            var options = new ClientOptions() { Key = ApiKey, UseBinaryProtocol = false };
            var client = new AblyRealtime(options);
            client.InitAuth().Wait();

            Config.Now = () => Now;
            return client;
        }

        private AblyRealtime GetClient()
        {
            Func<AblyRequest, Task<AblyResponse>> executeHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return Task.FromResult(new AblyResponse() { TextResponse = _dummyTokenResponse });
            };

            return GetClient(executeHttpRequest);
        }

        private static string GetKeyId()
        {
            return ApiKey.Split(':')[0];
        }
    }
}
