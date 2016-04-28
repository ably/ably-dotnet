﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.AuthTests
{
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
                return DummyTokenResponse.ToTask();
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
                return DummyTokenResponse.ToTask();
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
                opts.DefaultTokenParams = new TokenParams() { Ttl = TimeSpan.FromHours(2) };
                opts.AuthUrl = new Uri("http://authUrl");
                opts.AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } };
                opts.AuthParams = new Dictionary<string, string> { { "Test", "Test" }, { "TTl", "123" } };
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
                return JsonConvert.SerializeObject(new TokenRequest() { ClientId = "123" }).ToAblyResponse();
            }
            return DummyTokenResponse.ToTask();
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
                opts.AuthHeaders = new Dictionary<string, string> { { "default", "default" } };
                opts.AuthParams = new Dictionary<string, string> { { "default", "default" } };
            });

            var options = new AuthOptions
            {
                AuthUrl = new Uri("http://authUrl"),
                AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } },
                AuthParams = new Dictionary<string, string> { { "Test", "Test" } },
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
                opts.AuthHeaders = new Dictionary<string, string> { { "Test", "Test" } };
                opts.AuthParams = new Dictionary<string, string> { { "Test", "Test" }, { "Capability", "true" } };
                opts.AuthMethod = HttpMethod.Post;
            });

            var tokenParams = new TokenParams() { Capability = new Capability() };

            await rest.Auth.RequestToken(tokenParams, null);

            var expectedParams = new Dictionary<string, string>()
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
                return DummyTokenResponse.ToTask();
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

        private async Task SendRequestTokenWithValidOptions()
        {
            var rest = GetRestClient();
            var tokenParams = new TokenParams { Capability = new Capability(), ClientId = "ClientId", Ttl = TimeSpan.FromMinutes(10) };

            //Act
            await rest.Auth.RequestToken(tokenParams, null);
        }

        public RequestTokenSpecs(ITestOutputHelper output) : base(output)
        {
        }

    }
}
