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

        AblyResponse _dummyTokenResponse = new AblyResponse() { Type = ResponseType.Json, TextResponse = "{ \"access_token\": {}}" };

        private static string KeyId => ValidKey.Split(':')[0];

        [Fact]
        [Trait("spec", "RSA1")]
        public async Task WithTlsFalseAndBasicAuth_Throws()
        {
            var client = GetRestClient(setOptionsAction: options => { options.Tls = false; });
            await Assert.ThrowsAsync<InsecureRequestException>(() => client.Auth.Authorise(null, null, false));
        }

        [Fact]
        public void UsesKeyIdFromTheClient()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            data.KeyName.Should().Be(client.Options.ParseKey().KeyName);
        }

        [Fact]
        [Trait("spec", "RSA5")]
        public void UsesDefaultTtlWhenNoneIsSpecified()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            data.Ttl.Should().Be(Defaults.DefaultTokenTtl.TotalMilliseconds.ToString());
        }

        [Fact]
        [Trait("spec", "RSA6")]
        public void UsesTheDefaultCapability()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            data.Capability.Should().Be(Defaults.DefaultTokenCapability.ToJson());
        }

        [Fact]
        public void UsesUniqueNonseWhichIsMoreThan16Characters()
        {
            var client = GetRestClient();
            var data = client.Auth.CreateTokenRequest(null, null).Result;
            var secondTime = client.Auth.CreateTokenRequest(null, null).Result;
            data.Nonce.Should().NotBe(secondTime.Nonce);
            data.Nonce.Length.Should().BeGreaterOrEqualTo(16);
        }


        [Trait("spec", "RSA9b")]
        [Trait("spec", "RSA9i")]
        public class CreateTokenRequestAuthOptionSpecs : AuthorisationTests
        {
            public CreateTokenRequestAuthOptionSpecs(ITestOutputHelper output) : base(output)
            {
            }

            private AblyRest GetClientWithTokenParams()
            {
                return GetRestClient(null, options =>
                {
                    options.DefaultTokenParams = new TokenParams()
                    {
                        Ttl = TimeSpan.FromHours(2),
                        Capability = Capability.AllowAll,
                        ClientId = "123",
                        Timestamp = Now.AddMinutes(1),
                        Nonce = "defaultnonce"
                    };
                });
            }

            [Fact]
            public async Task WithDefaultTokenParams_ShouldSetTokenRequestValuesCorrectly()
            {
                var client = GetClientWithTokenParams();

                var request = await client.Auth.CreateTokenRequest();
                request.Capability.Should().Be(Capability.AllowAll.ToJson());
                request.ClientId.Should().Be("123");
                request.KeyName.Should().Be(ApiKey.Parse(client.Options.Key).KeyName);
                request.Ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
                request.Timestamp.Should().Be(Now.AddMinutes(1).ToUnixTimeInMilliseconds().ToString());
                request.Nonce.Should().Be("defaultnonce");
            }

            [Fact]
            public async Task WithOverrideTokenParams_ShouldSetTokenRequestValuesCorrectly()
            {
                var client = GetClientWithTokenParams();

                var overridingTokenParams = new TokenParams()
                {
                    Ttl = TimeSpan.FromHours(1),
                    ClientId = "999",
                    Capability = new Capability(),
                    Nonce = "overrideNonce",
                    Timestamp = Now.AddMinutes(10)
                };

                var request = await client.Auth.CreateTokenRequest(overridingTokenParams);

                request.Capability.Should().Be("");
                request.ClientId.Should().Be("999");
                request.Ttl.Should().Be(TimeSpan.FromHours(1).TotalMilliseconds.ToString());
                request.Timestamp.Should().Be(Now.AddMinutes(10).ToUnixTimeInMilliseconds().ToString());
                request.Nonce.Should().Be("overrideNonce");
            }

            [Fact]
            public async Task WithOverrideAuthOptions_ShouldSetTokenRequestValuesCorrectly()
            {
                var client = GetClientWithTokenParams();

                var overrideAuthOptions = new AuthOptions()
                {
                    Key = "keyid.name:secret",
                };

                var request = await client.Auth.CreateTokenRequest(null, overrideAuthOptions);

                request.KeyName.Should().Be("keyid.name");
            }
        }

        public class CreateTokenRequestSpecs : AuthorisationTests
        {
            [Fact]
            [Trait("spec", "RSA9h")]
            public async Task ReturnsASignedTokenRequest()
            {
                var request = await Client.Auth.CreateTokenRequest();
                request.Should().NotBeNull();
                request.Mac.Should().NotBeEmpty();
            }

            [Fact]
            [Trait("spec", "RSA9c")]
            public async Task WithEmptyNonce_ShouldGenerateANonceWithMoreThan16Characters()
            {
                var request =await Client.Auth.CreateTokenRequest();
                request.Nonce.Length.Should().BeGreaterThan(16, "Nonce should be more than 16 characters");
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public async Task WithNoTimeStapmInRequest_ShouldUseSystemType()
            {
                var request = await Client.Auth.CreateTokenRequest();
                request.Timestamp.Should().Be(Now.ToUnixTimeInMilliseconds().ToString());
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public void WithTimeStampOverridesDefault()
            {
                var date = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var data = Client.Auth.CreateTokenRequest(new TokenParams() { Timestamp = date }, null).Result;
                data.Timestamp.Should().Be(date.ToUnixTimeInMilliseconds().ToString());
            }

            [Fact]
            [Trait("spec", "RSA9d")]
            public void WithQueryTimeQueriesForTimestamp()
            {
                var currentTime = Config.Now().ToUnixTimeInMilliseconds();
                var client = GetRestClient(x => ("[" + currentTime + "]").ToAblyJsonResponse());

                var data = client.Auth.CreateTokenRequest(null, new AuthOptions() { QueryTime = true }).Result;
                data.Timestamp.Should().Be(currentTime.ToString());
            }

            [Fact]
            [Trait("spec", "RSA9e")]
            public void WithTtlOverridesDefault()
            {
                var data = Client.Auth.CreateTokenRequest(new TokenParams { Ttl = TimeSpan.FromHours(2) }, null).Result;

                data.Ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
            }

            [Fact]
            [Trait("spec", "RSA9f")]
            public async Task WithCapabilitySpecifiedInTokenParams_ShouldPassTheJsonStringToRequest()
            {
                var capability = new Capability();
                capability.AddResource("a").AllowAll();
                var customParams = new TokenParams() { Capability = capability };
                var request = await Client.Auth.CreateTokenRequest(customParams);
                request.Capability.Should().Be(capability.ToJson());
            }

            [Fact]
            [Trait("spec", "RSA9g")]
            public async Task GeneratesHMac()
            {
                var data = await Client.Auth.CreateTokenRequest();
                data.Mac.Should().NotBeEmpty();
            }

            [Fact]
            public void WithNonceOverridesDefault()
            {
                var data = Client.Auth.CreateTokenRequest(new TokenParams() { Nonce = "Blah" }, null).Result;
                data.Nonce.Should().Be("Blah");
            }

            [Fact]
            public void WithClientIdOverridesDefault()
            {
                var data = Client.Auth.CreateTokenRequest(new TokenParams() { ClientId = "123" }, null).Result;
                data.ClientId.Should().Be("123");
            }

            [Fact]
            public async void WithOutKeyIdThrowsException()
            {
                var client = new AblyRest(new ClientOptions());
                await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequest(null, null));
            }

            [Fact]
            public async void WithOutKeyValueThrowsException()
            {
                var client = new AblyRest(new ClientOptions() { Key = "111.222" });
                await Assert.ThrowsAsync<AblyException>(() => client.Auth.CreateTokenRequest(null, null));
            }

            

            public CreateTokenRequestSpecs(ITestOutputHelper output) : base(output)
            {
                Client = GetRestClient();
            }

            public AblyRest Client { get; }
        }

        

        [Fact]
        public void CreateTokenRequest_ShouldReturnASignedTokenRequest()
        {
            
        }

        [Fact]
        [Trait("spec", "RSA14")]
        public async Task WithNoTokenOrWayToGenerateOneAndUseTokenAuthIsTrue_AuthoriseShouldThrow()
        {
            var client = GetRestClient(setOptionsAction: options =>
            {
                options.Key = "";
                options.UseTokenAuth = true;
            });

            var ex = await Assert.ThrowsAsync<AblyException>(() => client.Auth.Authorise(null, null, false));
            ex.ErrorInfo.message.Should().Be("TokenAuth is on but there is no way to generate one");
        }

        public class ClientIdSpecs : AuthorisationTests
        {
            private string _clientId = "123";
            private string _wildcardClientId = "*";

            public ClientIdSpecs(ITestOutputHelper output) : base(output)
            {

            }

            private AblyRest GetRestClientWithClientId()
            {
                return GetRestClient(null, options => options.ClientId = _clientId);
            }

            [Fact]
            [Trait("spec", "RSA7a1")]
            public void ShouldNotPassClientIdWithPublishedMessage()
            {
                var client = GetRestClientWithClientId();

                client.Channels["test"].Publish("boo", "baa");

                var message = (LastRequest.PostData as List<Message>).First();

                message.clientId.Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RSA7a4")]
            public void WithClientIdandDefaultOptionsParamsClientId_UsesOptionsClientIdWhenMakingTokenRequests()
            {
                var client = GetRestClient(null, options =>
                {
                    options.ClientId = "123";
                    options.DefaultTokenParams = new TokenParams() { ClientId = "999" };
                });

                client.Auth.Authorise(null, null, true);
                var tokenRequest = LastRequest.PostData as TokenRequest;
                tokenRequest.ClientId.Should().Be("123");
            }

            [Fact]
            [Trait("spec", "RSA12b")]
            public void WhenNoClientIdIsSpecified_AuthClientIdShouldBeNull()
            {
                var client = GetRestClient();
                client.AblyAuth.GetClientId().Should().BeNullOrEmpty();
            }

            [Fact]
            [Trait("spec", "RSC17")]
            [Trait("spec", "RSA7b1")]
            public void WhenClientIdInOptions_ShouldPassClientIdtoAblyAuth()
            {
                var options = new ClientOptions(ValidKey) { ClientId = "123" };
                var client = new AblyRest(options);
                client.AblyAuth.GetClientId().Should().Be(options.ClientId);
            }

            [Fact]
            [Trait("spec", "RSA7b4")]
            public void WhenClientIsInitialisedWithTokenDetails_AuthClientIdShouldBeTheSame()
            {
                var options = new ClientOptions() { TokenDetails = new TokenDetails() { ClientId = "*" } };
                var client = new AblyRest(options);
                client.AblyAuth.GetClientId().Should().Be("*");
            }

            [Fact]
            [Trait("spec", "RSA7c")]
            public void ClientIdInClientOptionsCannotBeWildCard()
            {
                Assert.Throws<InvalidOperationException>(() => new ClientOptions() { ClientId = "*" });
            }
        }

        public class RequestTokenSpecs : AuthorisationTests
        {
            [Fact]
            [Trait("spec", "RSA8e")]
            [Trait("spec", "RSA8a")]
            public void WithDefaultParamsAndNothingSpecifiedInMethod_UsesDefaultParams()
            {
                var client = GetRestClient(null,
                    options =>
                        options.DefaultTokenParams = new TokenParams() { ClientId = "123", Ttl = TimeSpan.FromHours(2) });

                client.Auth.RequestToken();
                var data = LastRequest.PostData as TokenRequest;
                data.ClientId.Should().Be("123");
                data.Ttl.Should().Be(TimeSpan.FromHours(2).TotalMilliseconds.ToString());
            }

            [Fact]
            [Trait("spec", "RSA8e")]
            public async Task WithNoTokenParamsAndNoExtraOptions_CreatesDefaultRequestWithIdClientIdAndBlankCapability()
            {
                var client = GetRestClient(null, options => options.ClientId = "Test");
                await client.Auth.RequestToken(null, null);

                var data = LastRequest.PostData as TokenRequest;
                data.KeyName.Should().Be(KeyId);
                data.Capability.Should().Be(Capability.AllowAll.ToJson());
                data.ClientId.Should().Be(client.Options.ClientId);
            }

            [Fact]
            [Trait("spec", "RSA8b")]
            public async Task WithDefaultTokenParamsAndTokenParamsSpecified_ShouldUseOnlyParamsPassedIntoTheMethod()
            {
                var client = GetRestClient(null,
                                    options => options.DefaultTokenParams = new TokenParams
                                    {
                                        ClientId = "123",
                                        Ttl = TimeSpan.FromHours(2)
                                    });
                var capability = new Capability();
                capability.AddResource("a").AllowAll();
                var methodParams = new TokenParams()
                {
                    Capability = capability,
                    ClientId = "999",
                    Ttl = TimeSpan.FromMinutes(1),
                    Nonce = "123",
                    Timestamp = Now.AddHours(1)
                };

                await client.Auth.RequestToken(methodParams);

                var data = LastRequest.PostData as TokenRequest;
                data.Capability.Should().Be(capability.ToJson());
                data.ClientId.Should().Be(methodParams.ClientId);
                data.Ttl.Should().Be(methodParams.Ttl.Value.TotalMilliseconds.ToString());
                data.Nonce.Should().Be(methodParams.Nonce);
                data.Timestamp.Should().Be(methodParams.Timestamp.Value.ToUnixTimeInMilliseconds().ToString());
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
            [Trait("spec", "RSA8d")]
            public async Task RequestToken_WithAuthCallback_RetrievesTokenFromCallback()
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
            [Trait("spec", "RSA8c")]
            [Trait("spec", "RSA8c1a")]
            [Trait("spec", "RSA8c2")]
            public async Task WithAuthUrlAndDefaultAuthMethod_SendsGetRequestToTheUrlAndPassesQueryParameters()
            {
                var rest = GetRestClient(AuthExecuteHttpRequest, opts =>
                {
                    opts.DefaultTokenParams = new TokenParams() {Ttl = TimeSpan.FromHours(2)};
                    opts.AuthUrl = new Uri("http://authUrl");
                    opts.AuthHeaders = new Dictionary<string, string> {{"Test", "Test"}};
                    opts.AuthParams = new Dictionary<string, string> {{"Test", "Test"}, {"TTl", "123"}};
                });
                
                

                //Act
                await rest.Auth.RequestToken(null, null);

                //Expected will be { "ttl" : "intvalue", "Test" :"Test" }
                var expectedAuthParams = new Dictionary<string, string>()
                {
                    {"ttl", TimeSpan.FromHours(2).TotalMilliseconds.ToString()},
                    {"Test", "Test" }
                };

                //Assert
                Assert.Equal(HttpMethod.Get, FirstRequest.Method);
                Assert.Equal(rest.Options.AuthHeaders, FirstRequest.Headers);
                Assert.Equal(expectedAuthParams, FirstRequest.QueryParameters);
            }

            private Task<AblyResponse> AuthExecuteHttpRequest(AblyRequest request)
            {
                if (request.Url.Contains("authUrl"))
                {
                    return JsonConvert.SerializeObject(new TokenRequest() { ClientId = "123"}).ToAblyResponse();
                }
                return _dummyTokenResponse.ToTask();
            }

            [Fact]
            [Trait("spec", "RSA8c")]
            [Trait("spec", "RSA8c3")]
            public async Task WithDefaultAuthParamsAndHeadersAndSpecifiedOnce_ShouldIgnoreTheDefaultOnesAndNowMergeThem()
            {
                var rest = GetRestClient(AuthExecuteHttpRequest, opts =>
                {
                    opts.DefaultTokenParams = new TokenParams() { Ttl = TimeSpan.FromHours(2) };
                    opts.AuthUrl = new Uri("http://authUrl");
                    opts.AuthHeaders = new Dictionary<string, string> {{"default", "default"}};
                    opts.AuthParams = new Dictionary<string, string> {{"default", "default"}};
                });

                var options = new AuthOptions
                {
                    AuthUrl = new Uri("http://authUrl"),
                    AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                    AuthParams = new Dictionary<string, string> { { "Test", "Test" }},
                };

                //Act
                await rest.Auth.RequestToken(null, options);

                //Expected will be { "ttl" : "intvalue", "Test" :"Test" }
                var expectedAuthParams = new Dictionary<string, string>()
                {
                    {"ttl", TimeSpan.FromHours(2).TotalMilliseconds.ToString()},
                    {"Test", "Test" }
                };

                //Assert
                Assert.Equal(HttpMethod.Get, FirstRequest.Method);
                Assert.Equal(options.AuthHeaders, FirstRequest.Headers);
                Assert.Equal(expectedAuthParams, FirstRequest.QueryParameters);
            }

            [Fact]
            [Trait("spec", "RSA8c")]
            [Trait("spec", "RSA8c1b")]
            public async Task WithAuthUrlAndAuthMethodPost_SendPostRequestToAuthUrlAndPassesPostParameters()
            {
                var rest = GetRestClient(AuthExecuteHttpRequest, opts =>
                {
                    opts.AuthUrl = new Uri("http://authUrl");
                    opts.AuthHeaders = new Dictionary<string, string> {{"Test", "Test"}};
                    opts.AuthParams = new Dictionary<string, string> {{"Test", "Test"}, {"Capability", "true"}};
                    opts.AuthMethod = HttpMethod.Post;
                });
                
                var tokenParams = new TokenParams() { Capability = new Capability() };

                await rest.Auth.RequestToken(tokenParams, null);

                var expectedParams = new Dictionary<string,string>()
                {
                    { "capability", "" }, //Duplicate param so the value from TokenParams takes precedence
                    { "Test", "Test" }
                };

                Assert.Equal(HttpMethod.Post, FirstRequest.Method);

                Assert.Equal(rest.Options.AuthHeaders, FirstRequest.Headers);
                Assert.Equal(expectedParams, FirstRequest.PostParameters);
                Assert.Equal(rest.Options.AuthUrl.ToString(), FirstRequest.Url);
            }

            [Fact]
            public async Task WithAuthUrlWhenTokenStringIsReturn_ReturnsToken()
            {
                var rest = GetRestClient(null, options => options.AuthUrl = new Uri("http://authUrl"));

                rest.ExecuteHttpRequest = (x) =>
                {
                    if (x.Url == rest.Options.AuthUrl.ToString())
                    {
                        return new AblyResponse
                        {
                            Type = ResponseType.Text,
                            TextResponse = "TokenString"
                        }.ToTask();
                    }
                    return "{}".ToAblyResponse();
                };

                var token = await rest.Auth.RequestToken();
                token.Token.Should().Be("TokenString");
            }

            [Fact]
            [Trait("spec", "RSA8c")]
            public async Task WithAuthUrlWhenTokenIsReturned_ReturnsToken()
            {
                var rest = GetRestClient();
                var options = new AuthOptions()
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
            public async Task WithAuthUrlTokenRequest_GetsResultAndPostToRetrieveToken()
            {
                var rest = GetRestClient();
                var options = new AuthOptions
                {
                    AuthUrl = new Uri("http://authUrl")
                };
                List<AblyRequest> requests = new List<AblyRequest>();
                var requestdata = new TokenRequest { KeyName = KeyId, Capability = "123", Mac = "mac" };
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

            public RequestTokenSpecs(ITestOutputHelper output) : base(output)
            {
            }

        }

        [Fact]
        public void TokenShouldNotBeSetBeforeAuthoriseIsCalled()
        {
            var client = GetRestClient();
            client.Auth.CurrentToken.Should().BeNull();
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
        [Trait("spec", "RSA10j")]
        public async Task Authorise_PreservesTokenRequestOptionsForSubsequentRequests()
        {
            var client = GetRestClient();
            await client.Auth.Authorise(new TokenParams() { Ttl = TimeSpan.FromMinutes(260) }, null, false);
            await client.Auth.Authorise(null, null, true);
            var data = LastRequest.PostData as TokenRequest;
            data.Ttl.Should().Be(TimeSpan.FromMinutes(260).TotalMilliseconds.ToString());
        }

        [Fact]
        [Trait("spec", "RSA10d")]
        public async Task Authorise_WithNotExpiredCurrentTokenAndForceTrue_RequestsNewToken()
        {
            var client = GetRestClient();
            client.Auth.CurrentToken = new TokenDetails() { Expires = Config.Now().AddHours(1) };

            var token = await client.Auth.Authorise(new TokenParams() { ClientId = "123", Capability = new Capability() }, null, true);

            Assert.Contains("requestToken", LastRequest.Url);
            token.Should().NotBeNull();
        }

        [Fact]
        [Trait("spec", "RSA10c")]
        public async Task Authorise_WithExpiredCurrentToken_RequestsNewToken()
        {
            var client = GetRestClient();
            var initialToken = new TokenDetails() { Expires = Config.Now().AddHours(-1) };
            client.Auth.CurrentToken = initialToken;

            var token = await client.Auth.Authorise(null, null, false);

            Assert.Contains("requestToken", LastRequest.Url);
            token.Should().NotBeSameAs(initialToken);
        }

        [Theory]
        [InlineData(Defaults.TokenExpireBufferInSeconds + 1, false)]
        [InlineData(Defaults.TokenExpireBufferInSeconds, true)]
        [InlineData(Defaults.TokenExpireBufferInSeconds - 1, true)]
        [Trait("spec", "RSA10c")]
        public async Task Authorise_WithTokenExpiringIn15Seconds_RenewsToken(int secondsLeftToExpire, bool shouldRenew)
        {
            var client = GetRestClient();
            var initialToken = new TokenDetails() { Expires = Now.AddSeconds(secondsLeftToExpire) };
            client.Auth.CurrentToken = initialToken;

            var token = await client.Auth.Authorise(null, null, false);
            
            if (shouldRenew)
            {
                Assert.Contains("requestToken", LastRequest.Url);
                token.Should().NotBeSameAs(initialToken);
            }
            else
            {
                token.Should().BeSameAs(initialToken);
            }
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
