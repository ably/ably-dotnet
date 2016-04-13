using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using IO.Ably.Auth;
using Newtonsoft.Json;
using Xunit;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class AuthorisationTests : MockHttpSpecs
    {
        internal override AblyResponse DefaultResponse => _dummyTokenResponse;
        
        AblyResponse _dummyTokenResponse = new AblyResponse() { TextResponse = "{ \"access_token\": {}}" };

        private static string KeyId => ValidKey.Split(':')[0];

        [Fact]
        [Trait("spec", "RSA1")]
        public void WithTlsFalseAndBasicAuth_Throws()
        {
            var client = GetRestClient(setOptionsAction: options => { options.Tls = false; });
            Assert.ThrowsAsync<InsecureRequestException>(() => client.Auth.Authorise(null, null, false));
        }

        [Fact]
        public void TokenShouldNotBeSetBeforeAuthoriseIsCalled()
        {
            var client = GetRestClient(request => _dummyTokenResponse.ToTask());
            client.Auth.CurrentToken.Should().BeNull();
        }

        [Fact]
        public async Task RequestToken_CreatesPostRequestWithCorrectUrl()
        {
            //Arrange
            await SendRequestTokenWithValidOptions();

            //Assert
            Assert.Equal("/keys/" + KeyId + "/requestToken", LastRequest.Url);
            Assert.Equal(HttpMethod.Post, LastRequest.Method);
        }

        [Fact]
        public async Task RequestToken_SetsRequestTokenRequestToRequestPostData()
        {
            await SendRequestTokenWithValidOptions();

            Assert.IsType<TokenRequest>(LastRequest.PostData);
        }

        [Fact]
        public void RequestToken_WithTokenRequestWithoutId_UsesRestClientDefaultKeyId()
        {
            var client = GetRestClient();

            client.Auth.RequestToken(new TokenParams(), null);

            var data = LastRequest.PostData as TokenRequest;
            Assert.Equal(client.Options.ParseKey().KeyName, data.KeyName);
        }

        [Fact]
        public async Task RequestToken_WithNoRequestAndNoExtraOptions_CreatesDefaultRequestWithIdClientIdAndBlankCapability()
        {
            var client = GetRestClient();
            client.Options.ClientId = "Test";
            await client.Auth.RequestToken(null, null);

            var data = LastRequest.PostData as TokenRequest;
            data.KeyName.Should().Be(KeyId);
            data.Capability.Should().Be(Capability.AllowAll.ToJson());
            data.ClientId.Should().Be(client.Options.ClientId);
        }

        [Fact]
        public async Task RequestToken_WithTokenRequestWithoutCapability_SetsBlankCapability()
        {
            var tokenParams = new TokenParams();

            var client = GetRestClient();

            await client.Auth.RequestToken(tokenParams, null);

            var data = LastRequest.PostData as TokenRequest;
            Assert.Equal(Capability.AllowAll.ToJson(), data.Capability);
        }

        [Fact]
        public async Task RequestToken_TimeStamp_SetsTimestampOnTheDataRequest()
        {
            var date = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var tokenParams = new TokenParams() { Timestamp = date };

            var client = GetRestClient();

            await client.Auth.RequestToken(tokenParams, null);

            var data = LastRequest.PostData as TokenRequest;
            Assert.Equal(date.ToUnixTimeInMilliseconds().ToString(), data.Timestamp);
        }

        [Fact]
        public async Task RequestToken_WithoutTimeStamp_SetsCurrentTimeOnTheRequest()
        {
            var tokenParams = new TokenParams();

            var client = GetRestClient();
            Output.WriteLine("Current time: " + Now.Ticks);
            await client.Auth.RequestToken(tokenParams, null);
            Output.WriteLine("Current time: " + Now.Ticks);

            var data = LastRequest.PostData as TokenRequest;
            Assert.Equal(Now.ToUnixTimeInMilliseconds().ToString(), data.Timestamp);
        }

        [Fact]
        public async Task RequestToken_WithQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var rest = GetRestClient();
            var currentTime = DateTimeOffset.UtcNow;
            rest.ExecuteHttpRequest = x =>
                {
                    if (x.Url.Contains("time"))
                        return ("[" + currentTime.ToUnixTimeInMilliseconds() + "]").ToAblyJsonResponse();

                    //Assert
                    var data = x.PostData as TokenRequest;
                    Assert.Equal(data.Timestamp, currentTime.ToUnixTimeInMilliseconds().ToString());
                    return _dummyTokenResponse.ToTask();
                };
            var tokenParams = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            await rest.Auth.RequestToken(tokenParams, new AuthOptions() { QueryTime = true });
        }

        [Fact]
        public async Task RequestToken_WithoutQueryTime_SendsTimeRequestAndUsesReturnedTimeForTheRequest()
        {
            var rest = GetRestClient();
            rest.ExecuteHttpRequest = x =>
            {
                Assert.False(x.Url.Contains("time"));
                return _dummyTokenResponse.ToTask();
            };

            var tokenParams = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            await rest.Auth.RequestToken(tokenParams, new AuthOptions() { QueryTime = false });
        }

        [Fact]
        public async Task RequestToken_WithRequestCallback_RetrievesTokenFromCallback()
        {
            var rest = GetRestClient();
            var tokenRequest = new TokenParams() { Capability = new Capability() };

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
                AuthUrl = new Uri("http://authUrl"),
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
            };

            AblyRequest authRequest = null;
            var requestdata = new TokenRequest { KeyName = KeyId, Capability = "123" };
            rest.ExecuteHttpRequest = x =>
            {
                if (x.Url == options.AuthUrl.ToString())
                {
                    authRequest = x;
                    return JsonConvert.SerializeObject(requestdata).ToAblyResponse();
                }
                return _dummyTokenResponse.ToTask();
            };

            var tokenRequest = new TokenParams { Capability = new Capability() };

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
                AuthUrl = new Uri("http://authUrl"),
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
                AuthMethod = HttpMethod.Post
            };
            AblyRequest authRequest = null;
            var requestdata = new TokenRequest { KeyName = KeyId, Capability = "123" };
            rest.ExecuteHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl.ToString())
                {
                    authRequest = x;
                    return JsonConvert.SerializeObject(requestdata).ToAblyResponse();
                }
                return _dummyTokenResponse.ToTask();
            };

            var tokenRequest = new TokenParams() { Capability = new Capability() };

            await rest.Auth.RequestToken(tokenRequest, options);

            Assert.Equal(HttpMethod.Post, authRequest.Method);

            Assert.Equal(options.AuthHeaders, authRequest.Headers);
            Assert.Equal(options.AuthParams, authRequest.PostParameters);
            Assert.Equal(options.AuthUrl.ToString(), authRequest.Url);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhenTokenIsReturned_ReturnsToken()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = new Uri("http://authUrl")
            };

            var dateTime = DateTimeOffset.UtcNow;
            rest.ExecuteHttpRequest = (x) =>
            {
                if (x.Url == options.AuthUrl.ToString())
                {
                    return ("{ " +
                                       "\"keyName\":\"123\"," +
                                       "\"expires\":" + dateTime.ToUnixTimeInMilliseconds() + "," +
                                       "\"issued\":" + dateTime.ToUnixTimeInMilliseconds() + "," +
                                       "\"capability\":\"{}\"," +
                                       "\"clientId\":\"111\"" +
                                       "}").ToAblyResponse();
                }
                return "{}".ToAblyResponse();
            };

            var tokenRequest = new TokenParams() { Capability = new Capability() };

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
                AuthUrl = new Uri("http://authUrl")
            };
            List<AblyRequest> requests = new List<AblyRequest>();
            var requestdata = new TokenRequest { KeyName = KeyId, Capability = "123" };
            rest.ExecuteHttpRequest = (x) =>
            {
                requests.Add(x);
                if (x.Url == options.AuthUrl.ToString())
                {
                    return JsonConvert.SerializeObject(requestdata).ToAblyResponse();
                }
                return _dummyTokenResponse.ToTask();
            };

            var tokenParams = new TokenParams() { Capability = new Capability() };

            await rest.Auth.RequestToken(tokenParams, options);

            Assert.Equal(2, requests.Count);
            Assert.Equal(requestdata, requests.Last().PostData);
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhichReturnsAnErrorThrowsAblyException()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = new Uri("http://authUrl")
            };

            rest.ExecuteHttpRequest = (x) => { throw new AblyException("Testing"); };

            var tokenParams = new TokenParams() { Capability = new Capability() };

            var ex = await Assert.ThrowsAsync<AblyException>(() => rest.Auth.RequestToken(tokenParams, options));
        }

        [Fact]
        public async Task RequestToken_WithAuthUrlWhichReturnsNonJsonContentType_ThrowsException()
        {
            var rest = GetRestClient();
            var options = new AuthOptions
            {
                AuthUrl = new Uri("http://authUrl")
            };
            rest.ExecuteHttpRequest = (x) => Task.FromResult(new AblyResponse { Type = ResponseType.Binary });

            var tokenParams = new TokenParams() { Capability = new Capability() };

            await Assert.ThrowsAsync<AblyException>(() => rest.Auth.RequestToken(tokenParams, options));
        }

        [Fact]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceFalse_ReturnsCurrentToken()
        {
            var client = GetRestClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Same(client.Auth.CurrentToken, token);
        }

        [Fact]
        public async Task Authorise_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetRestClient();
            await client.Auth.Authorise(new TokenParams() { Ttl = TimeSpan.FromMinutes(260) }, null, false);

            await client.Auth.Authorise(null, null, false);
            var data = LastRequest.PostData as TokenRequest;
            data.Ttl.Should().Be(TimeSpan.FromMinutes(260).TotalMilliseconds.ToString());
        }

        [Fact]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(new TokenParams() { ClientId = "123", Capability = new Capability() }, null, true);

            Assert.Contains("requestToken", LastRequest.Url);
            token.Should().NotBeNull();
        }

        [Fact]
        public async Task Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetRestClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(-1) };

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Contains("requestToken", LastRequest.Url);
            token.Should().NotBeNull();
        }

        private async Task SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var tokenParams = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            await rest.Auth.RequestToken(tokenParams, null);
        }

        public AuthorisationTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
