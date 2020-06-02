﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.AcceptanceTests;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class RestSpecs : MockHttpRestSpecs
    {
        public class WhenInitialisingRestClient
        {
            [Fact]
            [Trait("spec", "RSC1")]
            public void WithInvalidKey_ThrowsAnException()
            {
                // Needs to have ':' because otherwise it's considered a token
                Assert.Throws<AblyException>(() => new AblyRest("InvalidKey:boo"));
            }

            [Fact]
            [Trait("spec", "RSC1")]
            public void WithValidKey_InitialisesTheClient()
            {
                var client = new AblyRest(ValidKey);
                Assert.NotNull(client);
            }

            [Fact]
            [Trait("spec", "RSC1")]
            public void WithKeyInOptions_InitialisesTheClient()
            {
                var client = new AblyRest(new ClientOptions(ValidKey));
                Assert.NotNull(client);
            }

            [Fact]
            [Trait("spec", "RSC1")]
            public void Ctor_WithKeyPassedInOptions_InitializesClient()
            {
                var client = new AblyRest(opts => opts.Key = ValidKey);
                Assert.NotNull(client);
            }

            [Fact]
            [Trait("spec", "RSC1")]
            public void WithTokenAndClientId_InitializesClient()
            {
                var client = new AblyRest(opts =>
                {
                    opts.Token = "blah";
                    opts.ClientId = "123";
                });

                Assert.Equal(AuthMethod.Token, client.AblyAuth.AuthMethod);
            }

            [Theory]
            [Trait("spec", "RSC1a")]
            [InlineData(AblySpecs.ValidKey, false)]
            [InlineData("fake token", true)]
            [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", true)]
            [InlineData("boo.Boo:boo", false)] // It determines whether it's a key based on ':'
            public void WithAString_ShouldRecogniseBetweenKeyAndToken(string key, bool isToken)
            {
                var client = new AblyRest(key);

                if (isToken)
                {
                    client.Options.Token.Should().Be(key);
                }
                else
                {
                    client.Options.Key.Should().Be(key);
                }
            }
        }

        [Fact]
        [Trait("spec", "RSC2")]
        public void DefaultLoggerSinkShouldbeSetup()
        {
            var logger = new DefaultLogger.InternalLogger();
            logger.LoggerSink.Should().BeOfType<DefaultLoggerSink>();
        }

        [Fact]
        [Trait("spec", "RSC3")]
        public void DefaultLogLevelShouldBeWarning()
        {
            var logger = new DefaultLogger.InternalLogger();
            logger.LogLevel.Should().Be(LogLevel.Warning);
        }

        [Fact]
        [Trait("spec", "RSC4")]
        public void ACustomLoggerCanBeProvided()
        {
            var logger = new DefaultLogger.InternalLogger();
            var sink = new TestLoggerSink();
            logger.LoggerSink = sink;
            logger.Error("Boo");

            sink.LastLevel.Should().Be(LogLevel.Error);
            sink.LastMessage.Should().Contain("Boo");
        }

        [Fact]
        [Trait("spec", "RSC5")]
        public void RestClientProvidesAccessToAuthObjectInstantiatedWithSameOptionsPassedToRestConstructor()
        {
            // Arrange
            var options = new ClientOptions(ValidKey);

            // Act
            var client = new AblyRest(options);

            // Assert
            var auth = client.Auth as AblyAuth;
            auth.Options.Should().BeSameAs(options);
        }

        [Theory]
        [Trait("spec", "RSC7")]
        [Trait("spec", "RSC18")]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldInitialiseAblyHttpClientWithCorrectTlsValue(bool tls)
        {
            var client = new AblyRest(new ClientOptions(ValidKey) { Tls = tls });
            client.HttpClient.Options.IsSecure.Should().Be(tls);
        }

        [Fact]
        [Trait("spec", "RSC8a")]

        public void ShouldUseBinaryProtocolByDefault()
        {
            if (!Defaults.MsgPackEnabled)
            {
                return;
            }

#pragma warning disable 162
            var client = new AblyRest(ValidKey);
            client.Options.UseBinaryProtocol.Should().BeTrue();
            client.Protocol.Should().Be(Defaults.Protocol);
#pragma warning restore 162
        }

        [Fact]
        [Trait("spec", "RSC8b")]
        public void WhenBinaryProtoclIsFalse_ShouldSetProtocolToJson()
        {
            var client = GetRestClient(setOptionsAction: opts => opts.UseBinaryProtocol = false);
            client.Protocol.Should().Be(Protocol.Json);
        }

        public class WithInvalidToken : MockHttpRestSpecs
        {
            private TokenDetails _returnedDummyTokenDetails;
            private bool _firstAttempt = true;

            [Theory]
            [InlineData(Defaults.TokenErrorCodesRangeStart)]
            [InlineData(Defaults.TokenErrorCodesRangeStart + 1)]
            [InlineData(Defaults.TokenErrorCodesRangeEnd)]
            [Trait("spec", "RSC10")]
            [Trait("intermittent", "true")]
            public async Task WhenErrorCodeIsTokenSpecific_ShouldAutomaticallyTryToRenewTokenIfRequestFails(int errorCode)
            {
                // Now = DateTimeOffset.Now;
                var tokenDetails = new TokenDetails("id") { Expires = Now.AddHours(1) };

                // Had to inline the method otherwise the tests intermittently fail.
                bool firstAttempt = true;
                var client = GetRestClient(
                    request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                    }

                    if (firstAttempt)
                    {
                        firstAttempt = false;
                        throw new AblyException(new ErrorInfo(string.Empty, errorCode, HttpStatusCode.Unauthorized));
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                }, opts => opts.TokenDetails = tokenDetails);

                await client.StatsAsync();

                client.AblyAuth.CurrentToken.Expires.Should().BeCloseTo(_returnedDummyTokenDetails.Expires);
                client.AblyAuth.CurrentToken.ClientId.Should().Be(_returnedDummyTokenDetails.ClientId);
            }

            [Fact]
            public async Task WhenErrorCodeIsNotTokenSpecific_ShouldThrow()
            {
                var client = GetConfiguredRestClient(40100, null);

                await Assert.ThrowsAsync<AblyException>(() => client.StatsAsync());
            }

            private AblyRest GetConfiguredRestClient(int errorCode, TokenDetails tokenDetails, bool useApiKey = true)
            {
                var client = GetRestClient(
                    request =>
                {
                    if (request.Url.Contains("/keys"))
                    {
                        return _returnedDummyTokenDetails.ToJson().ToAblyResponse();
                    }

                    if (_firstAttempt)
                    {
                        _firstAttempt = false;
                        throw new AblyException(new ErrorInfo(string.Empty, errorCode, HttpStatusCode.Unauthorized));
                    }

                    return AblyResponse.EmptyResponse.ToTask();
                }, opts =>
                {
                    opts.TokenDetails = tokenDetails;
                    if (useApiKey == false)
                    {
                        opts.Key = string.Empty;
                    }
                });
                return client;
            }

            public WithInvalidToken(ITestOutputHelper output)
                : base(output)
            {
                _returnedDummyTokenDetails = new TokenDetails("123") { Expires = Now.AddDays(1), ClientId = "123" };
            }
        }

        [Trait("spec", "RSC11")]
        public class HostSpecs : AblySpecs
        {
            private readonly FakeHttpMessageHandler _handler;

            public HostSpecs(ITestOutputHelper output)
                : base(output)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("12345678") };
                _handler = new FakeHttpMessageHandler(response);
            }

            private AblyRest CreateClient(Action<ClientOptions> optionsClient)
            {
                var options = new ClientOptions(ValidKey);
                optionsClient(options);
                var client = new AblyRest(options);
                client.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(10), _handler);
                return client;
            }

            [Fact]
            public async Task WithoutHostSpecific_ShouldUseDefaultHost()
            {
                var client = CreateClient(options => { });
                await MakeAnyRequest(client);
                _handler.LastRequest.RequestUri.Host.Should().Be(Defaults.RestHost);
            }

            [Fact]
            [Trait("spec", "RSC12")]
            public async Task WithHostSpecifiedInOption_ShouldUseCustomHost()
            {
                var client = CreateClient(options => { options.RestHost = "www.test.com"; });
                await MakeAnyRequest(client);
                _handler.LastRequest.RequestUri.Host.Should().Be("www.test.com");
            }

            [Theory]
            [InlineData("sandbox")]
            public async Task WithEnvironmentAndNoCustomHost_ShouldPrefixEnvironment(string environment)
            {
                var client = CreateClient(options => { options.Environment = environment; });
                await MakeAnyRequest(client);
                var expected = environment.ToString().ToLower() + "-" + Defaults.RestHost;
                _handler.LastRequest.RequestUri.Host.Should().Be(expected);
            }

            [Fact]
            public async Task WithEnvironmentAndCustomHost_ShouldUseCustomHostAsIs()
            {
                var client = CreateClient(options =>
                {
                    options.Environment = "sandbox";
                    options.RestHost = "www.test.com";
                });
                await MakeAnyRequest(client);
                _handler.LastRequest.RequestUri.Host.Should().Be("www.test.com");
            }

            [Fact]
            [Trait("spec", "TO3b")]
            public void WithLogLevel_ShouldUseNewLogLevel()
            {
                CreateClient(options =>
                {
                    options.LogLevel = LogLevel.Warning;
                });

                Logger.LogLevel.Should().Be(LogLevel.Warning);
            }

            private class TestLogHandler : ILoggerSink
            {
                public void LogEvent(LogLevel level, string message) { }
            }

            [Fact]
            [Trait("spec", "TO3c")]
            public void WithLogHandler_ShouldUseNewLogHandler()
            {
                new AblyRest(new ClientOptions(ValidKey) { LogHander = new TestLogHandler() });

                Logger.LoggerSink.Should().BeOfType<TestLogHandler>();
            }

            [Fact]
            [Trait("spec", "TO3n")]
            public void ClientOptions_IdempotentPublishingDefaultToFalseForVersionsBelow1_1()
            {
                var clientOptions = new ClientOptions();

                // This test needs to change once we implement v1.2
                clientOptions.IdempotentRestPublishing.Should().BeFalse();
            }

            private static async Task MakeAnyRequest(AblyRest client)
            {
                await client.Channels.Get("boo").PublishAsync("boo", "baa");
            }
        }

        [Fact]
        [Trait("spec", "RSC13")]
        public void HttpRequestTimeoutShouldComeFromClientOptions()
        {
            var httpRequestTimeout = TimeSpan.FromMinutes(1);
            var client = new AblyRest(options =>
            {
                options.Key = ValidKey;
                options.HttpRequestTimeout = httpRequestTimeout;
            });

            client.HttpClient.Client.Timeout.Should().Be(httpRequestTimeout);
        }

        [Fact]
        [Trait("spec", "RSC14a")]
        [Trait("spec", "RSA11")]
        public async Task AddAuthHeader_WithBasicAuthentication_AddsCorrectAuthorizationHeader()
        {
            // Arrange
            var rest = new AblyRest(ValidKey);
            ApiKey key = ApiKey.Parse(ValidKey);
            var request = new AblyRequest("/test", HttpMethod.Get, Protocol.Json);
            var expectedValue = "Basic " + key.ToString().ToBase64();

            // Act
            await rest.AblyAuth.AddAuthHeader(request);

            // Assert
            var authHeader = request.Headers.First();
            authHeader.Key.Should().Be("Authorization");
            authHeader.Value.Should().Be(expectedValue);
        }

        [Fact]
        [Trait("spec", "RSA3b")]
        public async Task AddAuthHeader_WithTokenAuthentication_AddsCorrectAuthorizationHeader()
        {
            // Arrange
            var tokenValue = "TokenValue";
            var rest = new AblyRest(opts => opts.Token = tokenValue);
            var request = new AblyRequest("/test", HttpMethod.Get, Protocol.Json);
            var expectedValue = "Bearer " + tokenValue.ToBase64();

            // Act
            await rest.AblyAuth.AddAuthHeader(request);

            // Assert
            var authHeader = request.Headers.First();
            authHeader.Key.Should().Be("Authorization");
            authHeader.Value.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("spec", "RSA3a")]
        public async Task TokenAuthCanBeUsedOverHttpAndHttps(bool tls)
        {
            // Arrange
            var tokenValue = "TokenValue";
            var rest = new AblyRest(opts =>
            {
                opts.Token = tokenValue;
                opts.Tls = tls;
            });
            var request = new AblyRequest("/test", HttpMethod.Get, Protocol.Json);

            // Act
            await rest.AblyAuth.AddAuthHeader(request);

            // If it throws the test will fail
        }

        public class FallbackSpecs : AblySpecs
        {
            private readonly FakeHttpMessageHandler _handler;
            private readonly HttpResponseMessage _response;

            public FallbackSpecs(ITestOutputHelper output)
                : base(output)
            {
                _response = new HttpResponseMessage() { Content = new StringContent("1234") };
                _handler = new FakeHttpMessageHandler(_response);
            }

            private AblyRest CreateClient(Action<ClientOptions> optionsClient)
            {
                var options = new ClientOptions(ValidKey);
                optionsClient?.Invoke(options);
                var client = new AblyRest(options);
                client.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(10), _handler);
                return client;
            }

            [Fact]
            [Trait("spec", "RSC15b")]
            public async Task WithOverriddenRestHost_DoesNotRetryAndFailsImmediately()
            {
                _response.StatusCode = HttpStatusCode.BadGateway;
                var client = CreateClient(options => options.RestHost = "boo.com");

                var ex = await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));
                ex.ErrorInfo.StatusCode.Should().Be(_response.StatusCode);
                _handler.NumberOfRequests.Should().Be(1);
            }

            [Fact]
            [Trait("spec", "RSC15b")]
            public async Task ShouldRetryRequestANumberOfTimes()
            {
                _response.StatusCode = HttpStatusCode.BadGateway;
                var client = CreateClient(null);

                var ex = await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));

                // ex.ErrorInfo.statusCode.Should().Be(_response.StatusCode);
                _handler.NumberOfRequests.Should().Be(client.Options.HttpMaxRetryCount);
            }

            [Fact]
            [Trait("spec", "RSC15e")]
            public async Task ShouldAttemptHttpRequestsAgainstTheDefaultHost()
            {
                _response.StatusCode = HttpStatusCode.BadGateway;
                var client = CreateClient(null);

                await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));

                _handler.Requests.First().RequestUri.Host.Should().Be(Defaults.RestHost);
            }

            [Fact]
            [Trait("spec", "RSC15a")]
            public async Task ShouldAttemptFallbackHostsInRandomOrder()
            {
                int interations = 20;
                _response.StatusCode = HttpStatusCode.BadGateway;

                // The higher the retries the less chance the two lists will match
                // but as per RSC15a each host will only be tried once (there are currently 6 hosts)...
                List<string> firstAttemptHosts = new List<string>();
                for (int i = 0; i < interations; i++)
                {
                    var client = CreateClient(options => options.HttpMaxRetryCount = 10);
                    await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));
                    firstAttemptHosts.AddRange(_handler.Requests.Select(x => x.RequestUri.Host).ToList());
                    _handler.Requests.Clear();
                    await Task.Delay(10);
                }

                await Task.Delay(100);

                List<string> secondAttemptHosts = new List<string>();
                for (int i = 0; i < interations; i++)
                {
                    var client = CreateClient(options => options.HttpMaxRetryCount = 10);
                    await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));
                    secondAttemptHosts.AddRange(_handler.Requests.Select(x => x.RequestUri.Host).ToList());
                    _handler.Requests.Clear();
                    await Task.Delay(10);
                }

                firstAttemptHosts.JoinStrings().Should().NotBe(secondAttemptHosts.JoinStrings());

                Output.WriteLine("FirstTryHosts: " + firstAttemptHosts.JoinStrings());
                Output.WriteLine("SecondTryHosts: " + secondAttemptHosts.JoinStrings());
            }

            [Fact]
            [Trait("spec", "RSC15a")]
            public async Task WhenConfigValueIsSetToMoreThanAvailableHosts_ShouldOnlyRetryEachFallbackHostOnce()
            {
                _response.StatusCode = HttpStatusCode.BadGateway;
                var client = CreateClient(opts => opts.HttpMaxRetryCount = 10);

                await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));

                _handler.Requests.Count.Should().Be(Defaults.FallbackHosts.Length + 1); // First attempt is with rest.ably.io
            }

            [Fact]
            [Trait("spec", "RSC15a")]
            [Trait("spec", "TO3k6")]
            public async Task ShouldUseCustomFallbackHostIfProvided()
            {
                _response.StatusCode = HttpStatusCode.BadGateway;
                List<string> attemptedList = new List<string>();

                var client = CreateClient(options => options.FallbackHosts = new[] { "www.example.com" });
                await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));
                attemptedList.AddRange(_handler.Requests.Select(x => x.RequestUri.Host).ToList());

                attemptedList.Count.Should().Be(2);
                attemptedList[0].Should().Be("rest.ably.io");
                attemptedList[1].Should().Be("www.example.com");
            }

            [Fact]
            [Trait("spec", "RSC15a")]
            [Trait("spec", "TO3k6")]
            public async Task ShouldNotUseAnyFallbackHostsIfEmptyArrayProvided()
            {
                _response.StatusCode = HttpStatusCode.BadGateway;
                List<string> attemptedList = new List<string>();

                var client = CreateClient(options => options.FallbackHosts = new string[] { });
                await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));
                attemptedList.AddRange(_handler.Requests.Select(x => x.RequestUri.Host).ToList());

                attemptedList.Count.Should().Be(1);
                attemptedList[0].Should().Be("rest.ably.io");
            }

            [Fact]
            [Trait("spec", "RSC15a")]
            [Trait("spec", "TO3k6")]
            public async Task ShouldUseDefaultFallbackHostsIfNullArrayProvided()
            {
                List<string> attemptedList = new List<string>();

                var client = CreateClient(options =>
                {
                    options.FallbackHosts = null;
                });

                var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadGateway));
                client.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(6), handler);

                await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));
                attemptedList.AddRange(handler.Requests.Select(x => x.RequestUri.Host).ToList());

                attemptedList.Count.Should().Be(3); // HttpMaxRetryCount defaults to 3
                attemptedList[0].Should().Be("rest.ably.io");
                attemptedList[1].Should().EndWith("ably-realtime.com");
                attemptedList[2].Should().EndWith("ably-realtime.com");
                attemptedList[1].Should().NotBe(attemptedList[2]);
            }

            /// <summary>
            /// (TO3l6) httpMaxRetryDuration integer – default 10,000 (10s). Max elapsed time in which fallback host retries for HTTP requests will be attempted i.e. if the first default host attempt takes 5s, and then the subsequent fallback retry attempt takes 7s, no further fallback host attempts will be made as the total elapsed time of 12s exceeds the default 10s limit
            /// </summary>
            [Fact]
            [Trait("spec", "TO3l6")]
            public async Task ShouldOnlyRetryFallbackHostWhileTheTimeTakenIsLessThanHttpMaxRetryDuration()
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                Func<DateTimeOffset> nowFunc = () => now;
                var options = new ClientOptions(ValidKey) { HttpMaxRetryDuration = TimeSpan.FromSeconds(21), NowFunc = nowFunc };
                var client = new AblyRest(options);
                _response.StatusCode = HttpStatusCode.BadGateway;
                var handler = new FakeHttpMessageHandler(
                    _response,
                    () =>
                    {
                        // Tweak time to pretend 10 seconds have elapsed
                        now = now.AddSeconds(10);
                    });

                client.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(6), handler);

                var ex = await Assert.ThrowsAsync<AblyException>(() => MakeAnyRequest(client));

                handler.Requests.Count.Should().Be(3); // First attempt is with rest.ably.io
            }

            [Fact]
            [Trait("spec", "RSC15f")]
            public async Task WhenUsingAFallbackHost_AfterFallbackRetryTimeoutPasses_ShouldRetryMainHost()
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                Func<DateTimeOffset> nowFunc = () => now;
                var options = new ClientOptions(ValidKey) { FallbackRetryTimeout = TimeSpan.FromSeconds(10), NowFunc = nowFunc };
                var client = new AblyRest(options);
                var requestCount = 0;

                _response.StatusCode = HttpStatusCode.BadGateway;
                Func<HttpResponseMessage> getResponse = () =>
                {
                    try
                    {
                        switch (requestCount)
                        {
                            case 0:
                                return new HttpResponseMessage(HttpStatusCode.BadGateway);
                            case 1:
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            case 2:
                                now = now.AddSeconds(20);
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            default:
                                return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }
                    finally
                    {
                        requestCount++;
                    }
                };

                var handler = new FakeHttpMessageHandler(getResponse);

                client.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(6), handler);

                MakeAnyRequest(client); // This will generate 2 requests - 1 failed and 1 succeed
                MakeAnyRequest(client); // This will generate 1 request which should be using fallback host
                MakeAnyRequest(client); // This will generate 1 request which should be back to the default host

                handler.Requests.Count.Should().Be(4); // First attempt is with rest.ably.io
                var attemptedHosts = handler.Requests.Select(x => x.RequestUri.Host).ToList();
                attemptedHosts[0].Should().Be(Defaults.RestHost);
                attemptedHosts[1].Should().BeOneOf(Defaults.FallbackHosts);
                attemptedHosts[2].Should().BeOneOf(Defaults.FallbackHosts);
                attemptedHosts[3].Should().Be(Defaults.RestHost);
            }

            [Fact]
            [Trait("spec", "RSC15f")]
            public async Task WhenUsingAFallbackHost_IfPreferredFallbackFails_ShouldRetryMainHostFirst()
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                Func<DateTimeOffset> nowFunc = () => now;
                var options = new ClientOptions(ValidKey) { FallbackRetryTimeout = TimeSpan.FromSeconds(10), NowFunc = nowFunc };
                var client = new AblyRest(options);
                var requestCount = 0;

                _response.StatusCode = HttpStatusCode.BadGateway;
                Func<HttpResponseMessage> getResponse = () =>
                {
                    try
                    {
                        switch (requestCount)
                        {
                            case 0:
                                return new HttpResponseMessage(HttpStatusCode.BadGateway);
                            case 1:
                                return new HttpResponseMessage(HttpStatusCode.OK);
                            case 2:
                                return new HttpResponseMessage(HttpStatusCode.BadGateway);
                            default:
                                return new HttpResponseMessage(HttpStatusCode.OK);
                        }
                    }
                    finally
                    {
                        requestCount++;
                    }
                };

                var handler = new FakeHttpMessageHandler(getResponse);

                client.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(6), handler);

                MakeAnyRequest(client); // This will generate 2 requests - 1 failed and 1 succeed
                MakeAnyRequest(client); // This will generate 2 request one to fallback host and the next one to the default host
                MakeAnyRequest(client); // This will generate 1 request which should be back to the default host

                handler.Requests.Count.Should().Be(5); // First attempt is with rest.ably.io
                var attemptedHosts = handler.Requests.Select(x => x.RequestUri.Host).ToList();
                attemptedHosts[0].Should().Be(Defaults.RestHost);
                attemptedHosts[1].Should().BeOneOf(Defaults.FallbackHosts);
                attemptedHosts[2].Should().BeOneOf(Defaults.FallbackHosts);
                attemptedHosts[3].Should().Be(Defaults.RestHost);
                attemptedHosts[4].Should().Be(Defaults.RestHost);
            }

            private static async Task MakeAnyRequest(AblyRest client)
            {
                await client.Channels.Get("boo").PublishAsync("boo", "baa");
            }
        }

        public class AuthCallbackSpecs : AblySpecs
        {
            public AuthCallbackSpecs(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task WithCallback_ExecutesCallbackOnFirstRequest()
            {
                bool called = false;

                Func<TokenParams, Task<object>> callback = (x) =>
                {
                    called = true;
                    return Task.FromResult<object>(new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddHours(1) });
                };

                await GetClient(callback).StatsAsync();

                called.Should().BeTrue("Rest with Callback needs to request token using callback");
            }

            [Fact]
            public async Task WhenCallbackReturnsNull_ThrowsAblyException()
            {
                await Assert.ThrowsAsync<AblyException>(() =>
                 {
                     return GetClient(_ => Task.FromResult<object>(null)).StatsAsync();
                 });
            }

            [Fact]
            public async Task WhenCallbackReturnsAnObjectThatIsNotTokenRequestOrTokenDetails_ThrowsAblyException()
            {
                var objects = new object[] { new object(), string.Empty, new Uri("http://test") };
                foreach (var obj in objects)
                {
                    await Assert.ThrowsAsync<AblyException>(() =>
                    {
                        return GetClient(_ => Task.FromResult(obj)).StatsAsync();
                    });
                }
            }

            private static AblyRest GetClient(Func<TokenParams, Task<object>> authCallback)
            {
                var options = new ClientOptions
                {
                    AuthCallback = authCallback,
                    UseBinaryProtocol = false
                };

                var rest = new AblyRest(options);
                rest.ExecuteHttpRequest = delegate { return "[{}]".ToAblyResponse(); };
                return rest;
            }
        }

        [Fact]
        public async Task Init_WithAuthUrl_CallsTheUrlOnFirstRequest()
        {
            bool called = false;
            var options = new ClientOptions
            {
                AuthUrl = new Uri("http://testUrl"),
                UseBinaryProtocol = false
            };

            var rest = new AblyRest(options);

            rest.ExecuteHttpRequest = request =>
            {
                if (request.Url.Contains(options.AuthUrl.ToString()))
                {
                    called = true;
                    return "{}".ToAblyResponse();
                }

                if (request.Url.Contains("requestToken"))
                {
                    return ("{ \"access_token\": { \"expires\": \"" + DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeInMilliseconds() + "\"}}").ToAblyResponse();
                }

                return "[{}]".ToAblyResponse();
            };

            await rest.StatsAsync();

            Assert.True(called, "Rest with Callback needs to request token using callback");
        }

        [Fact]
        public async Task ClientWithExpiredTokenAutomaticallyCreatesANewOne()
        {
            bool newTokenRequested = false;
            var options = new ClientOptions
            {
                AuthCallback = (x) =>
                {
                    newTokenRequested = true;
                    return Task.FromResult<object>(new TokenDetails("new.token")
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(1)
                    });
                },
                UseBinaryProtocol = false,
                NowFunc = TestHelpers.NowFunc()
            };
            var rest = new AblyRest(options);
            rest.ExecuteHttpRequest = request => "[{}]".ToAblyResponse();
            rest.AblyAuth.CurrentToken = new TokenDetails() { Expires = DateTimeOffset.UtcNow.AddDays(-2) };

            await rest.StatsAsync();
            newTokenRequested.Should().BeTrue();
            rest.AblyAuth.CurrentToken.Token.Should().Be("new.token");
        }

        [Fact]
        public async Task ClientWithExistingTokenReusesItForMakingRequests()
        {
            var options = new ClientOptions
            {
                ClientId = "test",
                Key = "best",
                UseTokenAuth = true,
                UseBinaryProtocol = false,
                NowFunc = TestHelpers.NowFunc()
            };
            var rest = new AblyRest(options);
            var token = new TokenDetails("123") { Expires = DateTimeOffset.UtcNow.AddHours(1) };
            rest.AblyAuth.CurrentToken = token;

            rest.ExecuteHttpRequest = request =>
            {
                // Assert
                request.Headers["Authorization"].Should().Contain(token.Token.ToBase64());
                return "[{}]".ToAblyResponse();
            };

            await rest.StatsAsync();
            await rest.StatsAsync();
            await rest.StatsAsync();
        }

        [Fact]
        public void ChannelsGet_ReturnsNewChannelWithName()
        {
            var rest = GetRestClient();

            var channel = rest.Channels.Get("Test");

            Assert.Equal("Test", channel.Name);
        }

        public RestSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
