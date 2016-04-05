using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using IO.Ably.Auth;
using Newtonsoft.Json;
using Xunit;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    public class AuthorisationTests
    {
        private const string ApiKey = "123.456:789";
        internal AblyRequest CurrentRequest { get; set; }
        public readonly DateTime Now = new DateTime(2012, 12, 12, 10, 10, 10, DateTimeKind.Utc);
        const string _dummyTokenResponse = "{ \"access_token\": {}}";

        static Task<AblyResponse> dummyTokenResponse { get { return _dummyTokenResponse.ToAblyResponse(); } }

        private AblyRest GetRestClient()
        {
            var rest = new AblyRest(new AblyOptions() { Key = ApiKey, UseBinaryProtocol = false});
            rest.ExecuteHttpRequest = (request) =>
            {
                CurrentRequest = request;
                return dummyTokenResponse;
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
        public async Task RequestToken_CreatesPostRequestWithCorrectUrl()
        {
            //Arrange
            await SendRequestTokenWithValidOptions();

            //Assert
            Assert.Equal("/keys/" + GetKeyId() + "/requestToken", CurrentRequest.Url);
            Assert.Equal(HttpMethod.Post, CurrentRequest.Method);
        }

        [Fact]
        public async Task RequestToken_SetsRequestTokenRequestToRequestPostData()
        {
            await SendRequestTokenWithValidOptions();

            Assert.IsType<TokenRequestPostData>(CurrentRequest.PostData);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutId_UsesRestClientDefaultKeyId()
        {
            var request = new TokenRequest();

            var client = GetRestClient();

            client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(client.Options.ParseKey().KeyName, data.keyName);
        }

        [Fact]
        public async Task RequestToken_WithNoRequestAndNoExtraOptions_CreatesDefaultRequestWithIdClientIdAndBlankCapability()
        {
            var client = GetRestClient();
            client.Options.ClientId = "Test";
            await client.Auth.RequestToken(null, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(GetKeyId(), data.keyName);
            Assert.Equal(Capability.AllowAll.ToJson(), data.capability);
            Assert.Equal(client.Options.ParseKey().KeyName, data.clientId);
        }

        [Fact]
        public async Task RequestToken_WithTokenRequestWithoutCapability_SetsBlankCapability()
        {
            var request = new TokenRequest() { KeyName = "123" };

            var client = GetRestClient();

            await client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(Capability.AllowAll.ToJson(), data.capability);
        }

        [Fact]
        public async Task RequestToken_TimeStamp_SetsTimestampOnTheDataRequest()
        {
            var date = DateTime.SpecifyKind(new DateTime(2014, 1, 1), DateTimeKind.Utc);
            var request = new TokenRequest() { Timestamp = date };

            var client = GetRestClient();

            await client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(date.ToUnixTimeInMilliseconds().ToString(), data.timestamp);
        }

        [Fact]
        public async Task RequestToken_WithoutTimeStamp_SetsCurrentTimeOnTheRequest()
        {
            var request = new TokenRequest();

            var client = GetRestClient();

            await client.Auth.RequestToken(request, null);

            var data = CurrentRequest.PostData as TokenRequestPostData;
            Assert.Equal(Now.ToUnixTimeInMilliseconds().ToString(), data.timestamp);
        }

        

        [Fact]
        public async Task RequestToken_WithQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var rest = GetRestClient();
            var currentTime = DateTime.UtcNow;
            rest.ExecuteHttpRequest = x =>
                {
                    if( x.Url.Contains( "time" ) )
                        return ( "[" + currentTime.ToUnixTimeInMilliseconds() + "]" ).ToAblyJsonResponse();

                    //Assert
                    var data = x.PostData as TokenRequestPostData;
                    Assert.Equal(data.timestamp, currentTime.ToUnixTimeInMilliseconds().ToString());
                    return dummyTokenResponse;
                };
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), KeyName = GetKeyId() };

            //Act
            await rest.Auth.RequestToken(request, new AuthOptions() { QueryTime = true });
        }

        [Fact]
        public async Task RequestToken_WithoutQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var rest = GetRestClient();
            rest.ExecuteHttpRequest = x =>
            {
                Assert.False(x.Url.Contains("time"));
                return dummyTokenResponse;
            };

            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), KeyName = GetKeyId() };

            //Act
            await rest.Auth.RequestToken(request, new AuthOptions() { QueryTime = false });
        }

        [Fact]
        public async Task RequestToken_WithRequestCallback_RetrievesTokenFromCallback()
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
            var result = await rest.Auth.RequestToken(tokenRequest, options);

            Assert.True(authCallbackCalled);
            Assert.Same(token, result);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlAndDefaultAuthMethod_SendsGetRequestToTheUrl()
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
                    return JsonConvert.SerializeObject( requestdata ).ToAblyResponse();
                }
                return dummyTokenResponse;
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            //Act
            await rest.Auth.RequestToken(tokenRequest, options);

            //Assert
            Assert.Equal(HttpMethod.Get, authRequest.Method);
            Assert.Equal(options.AuthHeaders, authRequest.Headers);
            Assert.Equal(options.AuthParams, authRequest.QueryParameters);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrl_SendPostRequestToAuthUrl()
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
                    return JsonConvert.SerializeObject( requestdata ).ToAblyResponse();
                }
                return dummyTokenResponse;
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            await rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(HttpMethod.Post, authRequest.Method);

            Assert.Equal(options.AuthHeaders, authRequest.Headers);
            Assert.Equal(options.AuthParams, authRequest.PostParameters);
            Assert.Equal(options.AuthUrl, authRequest.Url);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhenTokenIsReturned_ReturnsToken()
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
                    return ( "{ " +
                                       "\"keyName\":\"123\"," +
                                       "\"expires\":" + dateTime.ToUnixTimeInMilliseconds() + "," +
                                       "\"issued\":" + dateTime.ToUnixTimeInMilliseconds() + "," +
                                       "\"capability\":\"{}\"," +
                                       "\"clientId\":\"111\"" +
                                       "}" ).ToAblyResponse();
                }
                return "{}".ToAblyResponse();
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            var token = await rest.Auth.RequestToken(tokenRequest, options);
            Assert.NotNull(token);
            dateTime.Should().BeWithin(TimeSpan.FromSeconds(1)).After(token.Issued);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrl_GetsResultAndPostToRetrieveToken()
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
                    return JsonConvert.SerializeObject( requestdata ).ToAblyResponse();
                }
                return dummyTokenResponse;
            };

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            await rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(2, requests.Count);
            Assert.Equal(requestdata, requests.Last().PostData);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhichReturnsAnErrorThrowsAblyException()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };

            rest.ExecuteHttpRequest = (x) => rest._httpClient.Execute(x);

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            await Assert.ThrowsAsync<AblyException>(() => rest.Auth.RequestToken(tokenRequest, options));
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhichReturnsNonJsonContentType_ThrowsException()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = "http://authUrl"
            };
            rest.ExecuteHttpRequest = ( x ) => Task.FromResult( new AblyResponse { Type = ResponseType.Binary } );

            var tokenRequest = new TokenRequest { KeyName = GetKeyId(), Capability = new Capability() };

            await Assert.ThrowsAsync<AblyException>(() => rest.Auth.RequestToken(tokenRequest, options));
        }

        [Fact]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Same(client.CurrentToken, token);
        }

        [Fact]
        public async Task Authorise_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetRestClient();
            await client.Auth.Authorise(new TokenRequest() {Ttl = TimeSpan.FromMinutes(260)}, null, false);

            await client.Auth.Authorise(null, null, false);
            var data = CurrentRequest.PostData as TokenRequestPostData;
            data.ttl.Should().Be(TimeSpan.FromMinutes(260).TotalMilliseconds.ToString());
        }

        [Fact]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(new TokenRequest() { ClientId = "123", Capability = new Capability(), KeyName = "123" }, null, true);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        [Fact]
        public async Task Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetRestClient();
            client.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(-1) };

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Contains("requestToken", CurrentRequest.Url);
            token.Should().NotBeNull();
        }

        private async Task SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var request = new TokenRequest { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10), KeyName = GetKeyId() };

            //Act
            await rest.Auth.RequestToken(request, null);
        }
    }
}
