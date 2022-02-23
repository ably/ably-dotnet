﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Assets.Tests.AblySandbox;
using Cysharp.Threading.Tasks;
using FluentAssertions;
using IO.Ably;
using IO.Ably.Realtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Assets.Tests.PlayMode
{
    [TestFixture]
    [Category("EditorPlayer")]
    public class AuthSandboxSpecs
    {
        private AblySandboxFixture _sandboxFixture;

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            _sandboxFixture = new AblySandboxFixture();
        }

        [UnitySetUp]
        public IEnumerator Init()
        {
            UnitySandbox = new UnitySandboxSpecs(_sandboxFixture);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnitySandbox.Dispose();
            yield return null;
        }

        public UnitySandboxSpecs UnitySandbox { get; set; }

        private static TokenParams CreateTokenParams(Capability capability, TimeSpan? ttl = null)
        {
            var res = new TokenParams();
            res.ClientId = "John";
            res.Capability = capability;
            if (ttl.HasValue)
            {
                res.Ttl = ttl.Value;
            }

            return res;
        }

        private string _errorUrl = "https://echo.ably.io/respondwith?status=500";

        public static T RunAsyncMethodSync<T>(Task<T> asyncTask)
        {
            return Task.Run(async () => await asyncTask).GetAwaiter().GetResult();
        }
        public static void RunAsyncMethodSync(Task asyncTask)
        {
            Task.Run(async () => await asyncTask).GetAwaiter().GetResult();
        }

        public static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) { throw task.Exception; }
        }

        public static IEnumerator Await(Func<Task> taskDelegate)
        {
            return Await(taskDelegate.Invoke());
        }

        static Protocol [] protocols = {Protocol.Json};
            
        [UnityTest]
        public IEnumerator RSA4Helper_RestClient_ShouldTrackRequests([ValueSource(nameof(protocols))] Protocol protocol) => UniTask.ToCoroutine(async () =>
        {
            var authClient = await UnitySandbox.GetRestClient(protocol);
            var token = await authClient.AblyAuth.RequestTokenAsync(new TokenParams {ClientId = "123"});
            var helper = new RSA4Helper(this);
            var restClient = await helper.GetRestClientWithRequests(protocol, token, invalidateKey: true);
            helper.Requests.Count.Should().Be(0);
            await restClient.TimeAsync();
            helper.Requests.Count.Should().Be(1);
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, token, invalidateKey: true);
            helper.Requests.Count.Should().Be(1);
            await realtimeClient.RestClient.TimeAsync();
            helper.Requests.Count.Should().Be(2);
        });

        [UnityTest]
        public IEnumerator RestClient_WhenTokenExpired_ShouldNotRetryAndRaiseError()
        {
            yield return Await(async () =>
            {
                await UnityTest_RestClient_WhenTokenExpired_ShouldNotRetryAndRaiseError();
            });
        }
        
        public async Task UnityTest_RestClient_WhenTokenExpired_ShouldNotRetryAndRaiseError(Protocol protocol = Protocol.Json)
        {
            var helper = new RSA4Helper(this);

            // Get a very short lived token and wait for it to expire
            var authClient = await UnitySandbox.GetRestClient(protocol);
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(1) });
            await Task.Delay(TimeSpan.FromMilliseconds(2));

            // Modify the expiry date to fool the client it has a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            // create a new client with the token
            // set the Key to an empty string to override the sandbox settings
            var restClient = await helper.GetRestClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            var now = DateTimeOffset.UtcNow;

            // check the client thinks the token is valid
            restClient.AblyAuth.CurrentToken.IsValidToken(now).Should().BeTrue();

            var channelName = "RSA4a".AddRandomSuffix();

            try
            {
                await restClient.Channels.Get(channelName).PublishAsync("event", "data");
                throw new Exception("Unexpected success, the preceding code should have raised an AblyException");
            }
            catch (AblyException e)
            {
                // the server responds with a token error
                // (401 HTTP status code and an Ably error value 40140 <= code < 40150)
                // As the token is expired we can expect a specific code "40142": "token expired"
                e.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
                e.ErrorInfo.Code.Should().Be(ErrorCodes.NoMeansProvidedToRenewAuthToken);
            }

            // did not retry the request
            helper.Requests.Count.Should().Be(1, "only one request should have been attempted");
            helper.Requests[0].Url.Should().Be($"/channels/{channelName}/messages", "only the publish request should have been attempted");
        }

        [UnityTest]
        public IEnumerator RealtimeClient_NewInstanceWithExpiredToken_ShouldNotRetryAndHaveError()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealtimeClient_NewInstanceWithExpiredToken_ShouldNotRetryAndHaveError();
            });
        }

        public async Task UnityTest_RealtimeClient_NewInstanceWithExpiredToken_ShouldNotRetryAndHaveError(Protocol protocol = Protocol.Json)
        {
            var helper = new RSA4Helper(this);
            var authClient = await UnitySandbox.GetRestClient(protocol);
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(1) });
            await Task.Delay(TimeSpan.FromMilliseconds(2));

            // Modify the expiry date to fool the client it has a valid token
            almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1);

            // get a realtime client with no key
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            bool connected = false;
            realtimeClient.Connection.Once(ConnectionEvent.Connected, (_) => { connected = true; });

            // assert that there is no pre-existing error
            realtimeClient.Connection.ErrorReason.Should().BeNull();

            await realtimeClient.WaitForState(ConnectionState.Failed);
            realtimeClient.Connection.State.Should().Be(ConnectionState.Failed);
            connected.Should().BeFalse();

            realtimeClient.Connection.ErrorReason.Code.Should().Be(ErrorCodes.NoMeansProvidedToRenewAuthToken);
            helper.Requests.Count.Should().Be(0);
        }

        [UnityTest]
        public IEnumerator RealtimeClient_ConnectedWithExpiringToken_WhenTokenExpired_ShouldNotRetryAndHaveError()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealtimeClient_ConnectedWithExpiringToken_WhenTokenExpired_ShouldNotRetryAndHaveError();
            });
        }

        public async Task UnityTest_RealtimeClient_ConnectedWithExpiringToken_WhenTokenExpired_ShouldNotRetryAndHaveError(Protocol protocol = Protocol.Json)
        {
            var helper = new RSA4Helper(this);

            // Create a token that is valid long enough for a successful connection to occur
            var authClient = await UnitySandbox.GetRestClient(protocol);
            var almostExpiredToken = await authClient.AblyAuth.RequestTokenAsync(new TokenParams { ClientId = "123", Ttl = TimeSpan.FromMilliseconds(8000) });

            // get a realtime client with no Key, AuthUrl, or authCallback
            var realtimeClient = await helper.GetRealTimeClientWithRequests(protocol, almostExpiredToken, invalidateKey: true);

            await realtimeClient.WaitForState(ConnectionState.Connected);

            // assert that there is no pre-existing error
            realtimeClient.Connection.ErrorReason.Should().BeNull();

            await realtimeClient.WaitForState(ConnectionState.Failed);
            realtimeClient.Connection.State.Should().Be(ConnectionState.Failed);

            realtimeClient.Connection.ErrorReason.Code.Should().Be(ErrorCodes.NoMeansProvidedToRenewAuthToken);
            helper.Requests.Count.Should().Be(0);
        }

        [UnityTest]
        public IEnumerator RealtimeWithAuthError_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealtimeWithAuthError_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError();
            });
        }

        public async Task UnityTest_RealtimeWithAuthError_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError(Protocol protocol = Protocol.Json)
        {
            var helper = new RSA4Helper(this);

            var restClient = await UnitySandbox.GetRestClient(protocol);
            var token = await restClient.Auth.AuthorizeAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000),
            });

            // this realtime client will have a key for the sandbox, thus a means to renew
            var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.AutoConnect = false;
            });

            realtimeClient.RestClient.ExecuteHttpRequest = helper.AblyResponseWith500Status;

            var awaiter = new TaskCompletionAwaiter(5000);

            realtimeClient.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                state.Reason.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
                awaiter.SetCompleted();
            });

            await Task.Delay(2000);
            realtimeClient.Connect();

            var result = await awaiter.Task;
            result.Should().BeTrue();
            helper.Requests.Count.Should().Be(1);
            helper.Requests[0].Url.EndsWith("requestToken").Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator RealTimeWithAuthCallback_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealTimeWithAuthCallback_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError();
            });
        }

        public async Task UnityTest_RealTimeWithAuthCallback_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError(Protocol protocol = Protocol.Json)
        {
            // create a short lived token
            var authRestClient = await UnitySandbox.GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000),
            });

            bool didRetry = false;
            var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.AuthCallback = tokenParams =>
                {
                    didRetry = true;
                    throw new Exception("AuthCallback failed");
                };
                options.AutoConnect = false;
            });

            var awaiter = new TaskCompletionAwaiter(5000);
            realtimeClient.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                state.Reason.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
                awaiter.SetCompleted();
            });

            await Task.Delay(2000);
            realtimeClient.Connect();

            var result = await awaiter.Task;
            result.Should().BeTrue();
            didRetry.Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator RealTimeWithAuthUrl_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealTimeWithAuthUrl_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError();
            });
        }

        public async Task UnityTest_RealTimeWithAuthUrl_WhenTokenExpired_ShouldRetry_WhenRetryFails_ShouldSetError(Protocol protocol = Protocol.Json)
        {
            var authRestClient = await UnitySandbox.GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000)
            });

            // this realtime client will have a key for the sandbox, thus a means to renew
            var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = token;
                options.AuthUrl = new Uri(_errorUrl);
                options.AutoConnect = false;
            });

            var awaiter = new TaskCompletionAwaiter(5000);
            realtimeClient.Connection.Once(ConnectionEvent.Disconnected, state =>
            {
                state.Reason.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
                awaiter.SetCompleted();
            });

            await Task.Delay(2000);
            realtimeClient.Connect();

            var result = await awaiter.Task;
            result.Should().BeTrue();
        }

        [UnityTest]
        public IEnumerator RealTimeWithAuthUrl_WhenTokenExpired_And_WithServerTime_ShouldRenewToken()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealTimeWithAuthUrl_WhenTokenExpired_And_WithServerTime_ShouldRenewToken();
            });
        }
        public async Task UnityTest_RealTimeWithAuthUrl_WhenTokenExpired_And_WithServerTime_ShouldRenewToken(Protocol protocol = Protocol.Json)
        {
            var authRestClient = await UnitySandbox.GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000),
            });

            // this realtime client will have a key for the sandbox, thus a means to renew
            var mainClient = await UnitySandbox.GetRestClient(protocol, options =>
            {
                options.QueryTime = true;
                options.TokenDetails = token;
            });
            await Task.Delay(2000);
            // This makes sure we get server time
            ((AblyAuth)mainClient.Auth).CreateTokenRequest();

            await mainClient.StatsAsync();
            ((AblyAuth)mainClient.Auth).CurrentToken.Should().NotBeSameAs(token);
        }

        [UnityTest]
        public IEnumerator RealTimeWithAuthUrl_WhenTokenExpired_And_WithServerTime_And_NoWayToRenewToken_ShouldErrorBeforeCallingServer()
        {
            yield return Await(async () =>
            {
                await UnityTest_RealTimeWithAuthUrl_WhenTokenExpired_And_WithServerTime_And_NoWayToRenewToken_ShouldErrorBeforeCallingServer();
            });
        }

        public async Task UnityTest_RealTimeWithAuthUrl_WhenTokenExpired_And_WithServerTime_And_NoWayToRenewToken_ShouldErrorBeforeCallingServer(Protocol protocol = Protocol.Json)
        {
            var authRestClient = await UnitySandbox.GetRestClient(protocol);
            var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
            {
                Ttl = TimeSpan.FromMilliseconds(1000),
            });

            // this realtime client will have a key for the sandbox, thus a means to renew
            var mainClient = await UnitySandbox.GetRestClient(protocol, options =>
            {
                options.Key = null;
                options.QueryTime = true;
                options.TokenDetails = token;
            });

            bool madeHttpCall = false;
            var previousExecuteRequest = mainClient.ExecuteHttpRequest;
            mainClient.ExecuteHttpRequest = request =>
            {
                if (request.Url != "/time")
                {
                    madeHttpCall = true;
                }

                return previousExecuteRequest(request);
            };
            await Task.Delay(2000);
            // This makes sure we get server time
            ((AblyAuth)mainClient.Auth).SetServerTime();

            var ex = await E7Assert.ThrowsAsync<AblyException>(mainClient.StatsAsync());
            ex.ErrorInfo.Should().BeSameAs(ErrorInfo.NonRenewableToken);
            madeHttpCall.Should().BeFalse();
        }


        [UnityTest]
        [Ignore("Test is failing for connecting assertion")]
        public IEnumerator Auth_WithRealtimeClient_WhenAuthFails_ShouldTransitionToOrRemainInTheCorrectState()
        {
            yield return Await(async () =>
            {
                await UnityTest_Auth_WithRealtimeClient_WhenAuthFails_ShouldTransitionToOrRemainInTheCorrectState();
            });
        }

        public async Task UnityTest_Auth_WithRealtimeClient_WhenAuthFails_ShouldTransitionToOrRemainInTheCorrectState(Protocol protocol = Protocol.Json)
        {
            async Task TestConnectingBecomesDisconnected(string context, Action<ClientOptions, TestEnvironmentSettings> optionsAction)
            {
                TaskCompletionAwaiter tca = new TaskCompletionAwaiter(5000);
                var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, optionsAction);
                realtimeClient.Connection.On(ConnectionEvent.Disconnected, change =>
                {
                    change.Previous.Should().Be(ConnectionState.Connecting);
                    change.Reason.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
                    tca.SetCompleted();
                });

                realtimeClient.Connection.Connect();
                await realtimeClient.ProcessCommands();

                (await tca.Task).Should().BeTrue(context);
            }

            // authUrl fails
            void AuthUrlOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthUrl = new Uri(_errorUrl);
                options.RealtimeRequestTimeout = TimeSpan.FromSeconds(2);
                options.HttpRequestTimeout = TimeSpan.FromSeconds(2);
            }

            // authCallback fails
            static void AuthCallbackOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthCallback = (tokenParams) => throw new Exception("AuthCallback force error");
            }

            // invalid token returned
            static void InvalidTokenOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthCallback = (tokenParams) => Task.FromResult<object>("invalid:token");
            }

            await TestConnectingBecomesDisconnected("With invalid AuthUrl connection becomes Disconnected", AuthUrlOptions);
            await TestConnectingBecomesDisconnected("With invalid AuthCallback Connection becomes Disconnected", AuthCallbackOptions);
            await TestConnectingBecomesDisconnected("With Invalid Token Connection becomes Disconnected", InvalidTokenOptions);

            /* RSA4c3 */

            async Task<TokenDetails> GetToken()
            {
                var authRestClient = await UnitySandbox.GetRestClient(protocol);
                var token = await authRestClient.Auth.RequestTokenAsync(new TokenParams
                {
                    Ttl = TimeSpan.FromMilliseconds(2000)
                });
                return token;
            }

            async Task TestConnectedStaysConnected(string context, Action<ClientOptions, TestEnvironmentSettings> optionsAction)
            {
                var token = await GetToken();
                token.Expires = DateTimeOffset.Now.AddMinutes(30);
                void Options(ClientOptions options, TestEnvironmentSettings settings)
                {
                    optionsAction(options, settings);
                    options.TokenDetails = token;
                }

                var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, Options);
                realtimeClient.Connect();
                await realtimeClient.WaitForState(ConnectionState.Connected);
                bool stateChanged = false;
                realtimeClient.Connection.On(change =>
                {
                    // this callback should not be called
                    stateChanged = true;
                });

                _ = await E7Assert.ThrowsAsync<AblyException>(realtimeClient.Auth.AuthorizeAsync());

                realtimeClient.Connection.State.Should().Be(ConnectionState.Connected, because: context);
                stateChanged.Should().BeFalse(because: context);
            }

            await TestConnectedStaysConnected("With invalid AuthUrl Connection remains Connected", AuthUrlOptions);
            await TestConnectedStaysConnected("With invalid AuthCallback connection remains Connected", AuthCallbackOptions);
            await TestConnectedStaysConnected("With Invalid Token connection remains Connected", InvalidTokenOptions);
        }

        [UnityTest]
        [NUnit.Framework.Property("spec", "RSA4d")]
        public IEnumerator Auth_WithRealtimeClient_WhenAuthFailsWith403_ShouldTransitionToFailed()
        {
            yield return Await(async () =>
            {
                await UnityTest_Auth_WithRealtimeClient_WhenAuthFailsWith403_ShouldTransitionToFailed();
            });
        }

        public async Task UnityTest_Auth_WithRealtimeClient_WhenAuthFailsWith403_ShouldTransitionToFailed(Protocol protocol = Protocol.Json)
        {
            async Task Test403BecomesFailed(string context, int expectedCode, Action<ClientOptions, TestEnvironmentSettings> optionsAction)
            {
                TaskCompletionAwaiter tca = new TaskCompletionAwaiter();
                var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, optionsAction);

                realtimeClient.Connection.Once(ConnectionEvent.Failed, change =>
                {
                    change.Previous.Should().Be(ConnectionState.Connecting);
                    change.Reason.Code.Should().Be(expectedCode);
                    realtimeClient.Connection.ErrorReason.Code.Should().Be(expectedCode);
                    realtimeClient.Connection.ErrorReason.StatusCode.Should().Be(HttpStatusCode.Forbidden); // 403
                    tca.SetCompleted();
                });

                realtimeClient.Connect();
                (await tca.Task).Should().BeTrue(context);
            }

            // authUrl fails and returns no body
            static void AuthUrlOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthUrl = new Uri("https://echo.ably.io/respondwith?status=403");
            }

            // AuthCallback that results in an ErrorInfo with code 403
            static void AuthCallbackOptions(ClientOptions options, TestEnvironmentSettings settings)
            {
                options.AutoConnect = false;
                options.AuthCallback = (tokenParams) =>
                {
                    var aex = new AblyException(new ErrorInfo("test", 40300, HttpStatusCode.Forbidden));
                    throw aex;
                };
            }

            await Test403BecomesFailed("With 403 response connection should become Failed", expectedCode: ErrorCodes.ClientAuthProviderRequestFailed, optionsAction: AuthUrlOptions);
            await Test403BecomesFailed("With ErrorInfo with StatusCode of 403 connection should become Failed", expectedCode: ErrorCodes.ClientAuthProviderRequestFailed, optionsAction: AuthCallbackOptions);
        }

        [NUnit.Framework.Property("spec", "RSA4d")]
        [NUnit.Framework.Property("spec", "RSA4d1")]
        [UnityTest]
        public IEnumerator Auth_WithRealtimeClient_WhenExplicitAuthFailsWith403_ShouldTransitionToFailed()
        {
            yield return Await(async () =>
            {
                await UnityTest_Auth_WithRealtimeClient_WhenExplicitAuthFailsWith403_ShouldTransitionToFailed();
            });
        }
        
        public async Task UnityTest_Auth_WithRealtimeClient_WhenExplicitAuthFailsWith403_ShouldTransitionToFailed(Protocol protocol = Protocol.Json)
        {
            var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol);
            await realtimeClient.WaitForState(ConnectionState.Connected);

            TaskCompletionAwaiter failedAwaiter = new TaskCompletionAwaiter();
            realtimeClient.Connection.Once(ConnectionEvent.Failed, change =>
            {
                change.Previous.Should().Be(ConnectionState.Connected);
                change.Reason.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
                realtimeClient.Connection.ErrorReason.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
                realtimeClient.Connection.ErrorReason.StatusCode.Should().Be(HttpStatusCode.Forbidden); // 403
                failedAwaiter.SetCompleted();
            });

            var authOptionsWhichFail = new AuthOptions
            {
                UseTokenAuth = true,
                AuthUrl = new Uri("https://echo.ably.io/respondwith?status=403"),
            };

            var ex = await E7Assert.ThrowsAsync<AblyException>(realtimeClient.Auth.AuthorizeAsync(null, authOptionsWhichFail));

            ex.Should().BeOfType<AblyException>();

            (await failedAwaiter.Task).Should().BeTrue("With 403 response connection should become Failed");
        }

        [NUnit.Framework.Property("spec", "RSA4d1")]
        [UnityTest]
        public IEnumerator Auth_WithConnectedRealtimeClient_WhenExplicitRequestTokenFailsWith403_ShouldNotAffectConnectionState()
        {
            yield return Await(async () =>
            {
                await UnityTest_Auth_WithConnectedRealtimeClient_WhenExplicitRequestTokenFailsWith403_ShouldNotAffectConnectionState();
            });
        }

        public async Task UnityTest_Auth_WithConnectedRealtimeClient_WhenExplicitRequestTokenFailsWith403_ShouldNotAffectConnectionState(Protocol protocol = Protocol.Json)
        {
            var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol);
            realtimeClient.Connection.Connect();

            await realtimeClient.WaitForState(ConnectionState.Connected);

            var authOptions = new AuthOptions
            {
                UseTokenAuth = true,
                AuthUrl = new Uri("https://echo.ably.io/respondwith?status=403"),
            };

            var ex = await E7Assert.ThrowsAsync<AblyException>(realtimeClient.Auth.RequestTokenAsync(null, authOptions));
            ex.Should().BeOfType<AblyException>();
            ex.ErrorInfo.Code.Should().Be(ErrorCodes.ClientAuthProviderRequestFailed);
            ex.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            await Task.Delay(1000);

            realtimeClient.Connection.State.Should().Be(ConnectionState.Connected);
        }

        
        [NUnit.Framework.Property("spec", "RSA8a")]
        [UnityTest]
        public IEnumerator ShouldReturnTheRequestedToken()
        {
            yield return Await(async () =>
            {
                await UnityTest_ShouldReturnTheRequestedToken();
            });
        }
        public async Task UnityTest_ShouldReturnTheRequestedToken(Protocol protocol = Protocol.Json)
        {
            var ttl = TimeSpan.FromSeconds(30 * 60);
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await UnitySandbox.GetRestClient(protocol);
            var options = ably.Options;

            var token = await ably.Auth.RequestTokenAsync(CreateTokenParams(capability, ttl), null);

            var key = options.ParseKey();
            var appId = key.KeyName.Split('.').First();
            token.Token.Should().MatchRegex($@"^{appId}\.[\w-]+$");
            token.Issued.Should().BeWithin(TimeSpan.FromSeconds(30)).Before(DateTimeOffset.UtcNow);
            token.Expires.Should().BeWithin(TimeSpan.FromSeconds(30)).Before(DateTimeOffset.UtcNow + ttl);
        }

        [NUnit.Framework.Property("spec", "RSA3a")]
        [UnityTest]
        public IEnumerator WithTokenId_AuthenticatesSuccessfullyOverHttpAndHttps()
        {
            yield return Await(async () =>
            {
                await UnityTest_WithTokenId_AuthenticatesSuccessfullyOverHttpAndHttps();
            });
        }

        public async Task UnityTest_WithTokenId_AuthenticatesSuccessfullyOverHttpAndHttps(Protocol protocol = Protocol.Json)
        {
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await UnitySandbox.GetRestClient(protocol);
            var token = await ably.Auth.RequestTokenAsync(CreateTokenParams(capability), null);

            var options = await _sandboxFixture.GetSettings();
            var httpTokenAbly =
                new AblyRest(new ClientOptions { Token = token.Token, Environment = options.Environment, Tls = false });
            var httpsTokenAbly =
                new AblyRest(new ClientOptions { Token = token.Token, Environment = options.Environment, Tls = true });

            // If it doesn't throw we are good :)
            await httpTokenAbly.Channels.Get("foo").PublishAsync("test", "true");
            await httpsTokenAbly.Channels.Get("foo").PublishAsync("test", "true");
        }

        
        [NUnit.Framework.Property("spec", "RSA4a2")]
        [UnityTest]
        public IEnumerator WithTokenAuth_WhenUnauthorizedErrorAndNoRenew_ShouldThrow40171AblyException()
        {
            yield return Await(async () =>
            {
                await UnityTest_WithTokenAuth_WhenUnauthorizedErrorAndNoRenew_ShouldThrow40171AblyException();
            });
        }

        public async Task UnityTest_WithTokenAuth_WhenUnauthorizedErrorAndNoRenew_ShouldThrow40171AblyException(Protocol protocol = Protocol.Json)
        {
            var ablyRest = await UnitySandbox.GetRestClient(protocol);
            var token = ablyRest.Auth.RequestToken(new TokenParams { Ttl = TimeSpan.FromSeconds(1) });

            await Task.Delay(2000);
            var ably = await UnitySandbox.GetRestClient(protocol, opts =>
            {
                opts.Key = string.Empty;
                opts.TokenDetails = token;
            });

            var ex = await E7Assert.ThrowsAsync<AblyException>(ably.StatsAsync());
            ex.ErrorInfo.Code.Should().Be(ErrorCodes.NoMeansProvidedToRenewAuthToken);
        }

        
        [UnityTest]
        [Ignore("Fails test with blocking async await")]
        public IEnumerator WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException()
        {
            yield return Await(async () =>
            {
                await UnityTest_WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException();
            });
        }

        public async Task UnityTest_WithTokenId_WhenTryingToPublishToUnspecifiedChannel_ThrowsAblyException(Protocol protocol = Protocol.Json)
        {
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var ably = await UnitySandbox.GetRestClient(protocol);

            var token = ably.Auth.RequestTokenAsync(CreateTokenParams(capability), null).Result;

            var tokenAbly = new AblyRest(new ClientOptions { Token = token.Token, Environment = "sandbox" });

            var error =
                await E7Assert.ThrowsAsync<AblyException>(tokenAbly.Channels.Get("boo").PublishAsync("test", "true"));
            error.ErrorInfo.Code.Should().Be(ErrorCodes.OperationNotPermittedWithCapability);
            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }


        [UnityTest]
        public IEnumerator WithInvalidTimeStamp_Throws()
        {
            yield return Await(async () =>
            {
                await UnityTest_WithInvalidTimeStamp_Throws();
            });
        }

        public async Task UnityTest_WithInvalidTimeStamp_Throws(Protocol protocol = Protocol.Json)
        {
            var ably = await UnitySandbox.GetRestClient(protocol);

            var tokenParams = CreateTokenParams(null);
            tokenParams.Timestamp = DateTimeOffset.UtcNow.AddDays(-1);
            var error = await E7Assert.ThrowsAsync<AblyException>(ably.Auth.RequestTokenAsync(tokenParams, AuthOptions.FromExisting(ably.Options).Merge(new AuthOptions { QueryTime = false })));

            error.ErrorInfo.Code.Should().Be(40104);
            error.ErrorInfo.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [NUnit.Framework.Property("spec", "RSA7b2")]
        [NUnit.Framework.Property("spec", "RSA10a")]
        [UnityTest]
        public IEnumerator WithoutClientId_WhenAuthorizedWithTokenParamsWithClientId_SetsClientId()
        {
            yield return Await(async () =>
            {
                await UnityTest_WithoutClientId_WhenAuthorizedWithTokenParamsWithClientId_SetsClientId();
            });
        }

        public async Task UnityTest_WithoutClientId_WhenAuthorizedWithTokenParamsWithClientId_SetsClientId(Protocol protocol = Protocol.Json)
        {
            var ably = await UnitySandbox.GetRestClient(protocol);
            var tokenDetails1 = await ably.Auth.AuthorizeAsync(new TokenParams { ClientId = "123" });
            ably.AblyAuth.ClientId.Should().Be("123");

            // uses Token Auth for all future requests (RSA10a)
            ably.AblyAuth.AuthMethod.Should().Be(AuthMethod.Token);

            // create a token immediately (RSA10a)
            // regardless of whether the existing token is valid or not
            var tokenDetails2 = await ably.Auth.AuthorizeAsync(new TokenParams { ClientId = "123" });
            tokenDetails1.Token.Should().NotBe(tokenDetails2.Token);
        }

        
        [NUnit.Framework.Property("spec", "RSA8f1")]
        [UnityTest]
        public IEnumerator TokenAuthWithoutClientId_ShouldNotSetClientIdOnMessagesAndTheClient()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthWithoutClientId_ShouldNotSetClientIdOnMessagesAndTheClient();
            });
        }

        public async Task UnityTest_TokenAuthWithoutClientId_ShouldNotSetClientIdOnMessagesAndTheClient(Protocol protocol = Protocol.Json)
        {
            var client = await UnitySandbox.GetRestClient(protocol, opts => opts.QueryTime = true);
            var settings = await _sandboxFixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync();
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            tokenClient.AblyAuth.ClientId.Should().BeNullOrEmpty();
            var channel = tokenClient.Channels["persisted:test".AddRandomSuffix()];
            await channel.PublishAsync("test", "test");
            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().BeNullOrEmpty();
            message.Data.Should().Be("test");
        }

        
        [NUnit.Framework.Property("spec", "RSA8f2")]
        [UnityTest]
        public IEnumerator TokenAuthWithoutClientIdAndAMessageWithExplicitId_ShouldThrow()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthWithoutClientIdAndAMessageWithExplicitId_ShouldThrow();
            });
        }

        public async Task UnityTest_TokenAuthWithoutClientIdAndAMessageWithExplicitId_ShouldThrow(Protocol protocol = Protocol.Json)
        {
            var client = await UnitySandbox.GetRestClient(protocol);
            var settings = await _sandboxFixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync();
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });
            await E7Assert.ThrowsAsync<AblyException>(tokenClient.Channels["test"].PublishAsync(new Message("test", "test") { ClientId = "123" }));
        }

        
        [NUnit.Framework.Property("spec", "RSA8f3")]
        [UnityTest]
        public IEnumerator TokenAuthWithWildcardClientId_ShouldPublishMessageSuccessfullyAndClientIdShouldBeSetToWildcard()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthWithWildcardClientId_ShouldPublishMessageSuccessfullyAndClientIdShouldBeSetToWildcard();
            });
        }

        public async Task UnityTest_TokenAuthWithWildcardClientId_ShouldPublishMessageSuccessfullyAndClientIdShouldBeSetToWildcard(
            Protocol protocol = Protocol.Json)
        {
            var client = await UnitySandbox.GetRestClient(protocol);
            var settings = await _sandboxFixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" });
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = tokenClient.Channels["pesisted:test"];
            await channel.PublishAsync("test", "test");
            tokenClient.AblyAuth.ClientId.Should().Be("*");
            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().BeNullOrEmpty();
            message.Data.Should().Be("test");
        }

        
        [NUnit.Framework.Property("spec", "RSA8f4")]
        [UnityTest]
        public IEnumerator TokenAuthWithWildcardClientId_WhenPublishingMessageWithClientId_ShouldExpectClientIdToBeSentWithTheMessage()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthWithWildcardClientId_WhenPublishingMessageWithClientId_ShouldExpectClientIdToBeSentWithTheMessage();
            });
        }

        public async Task
            UnityTest_TokenAuthWithWildcardClientId_WhenPublishingMessageWithClientId_ShouldExpectClientIdToBeSentWithTheMessage(
                Protocol protocol = Protocol.Json)
        {
            var client = await UnitySandbox.GetRestClient(protocol);
            var settings = await _sandboxFixture.GetSettings();
            var token = await client.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" });
            var tokenClient = new AblyRest(new ClientOptions
            {
                TokenDetails = token,
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = tokenClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });
            tokenClient.AblyAuth.ClientId.Should().Be("*");
            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        [UnityTest]
        public IEnumerator TokenAuthUrlWhenPlainTextTokenIsReturn_ShouldBeAblyToPublishWithNewToken()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthUrlWhenPlainTextTokenIsReturn_ShouldBeAblyToPublishWithNewToken();
            });
        }

        public async Task UnityTest_TokenAuthUrlWhenPlainTextTokenIsReturn_ShouldBeAblyToPublishWithNewToken(Protocol protocol = Protocol.Json)
        {
            var client = await UnitySandbox.GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" });
            var settings = await _sandboxFixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=text&body=" + token.Token;

            var authUrlClient = new AblyRest(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol
            });

            var channel = authUrlClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }


        [UnityTest]
        public IEnumerator TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToPublishWithNewToken()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToPublishWithNewToken();
            });
        }

        public async Task UnityTest_TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToPublishWithNewToken(Protocol protocol = Protocol.Json)
        {
            var client = await UnitySandbox.GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" });
            var settings = await _sandboxFixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=json&body=" + Uri.EscapeUriString(token.ToJson());

            var authUrlClient = new AblyRest(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol,
                HttpRequestTimeout = new TimeSpan(0, 0, 20)
            });

            var channel = authUrlClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }


        [UnityTest]
        public IEnumerator TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToConnect()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToConnect();
            });
        }

        public async Task UnityTest_TokenAuthUrlWithJsonTokenReturned_ShouldBeAbleToConnect(Protocol protocol = Protocol.Json)
        {
            var ablyRest = await UnitySandbox.GetRestClient(protocol);
            var token = await ablyRest.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" });
            var settings = await _sandboxFixture.GetSettings();
            var tokenJson = token.ToJson();
            var authUrl = "http://echo.ably.io/?type=json&body=" + Uri.EscapeUriString(tokenJson);

            var client = new AblyRealtime(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol,
                HttpRequestTimeout = new TimeSpan(0, 0, 20),
                AutomaticNetworkStateMonitoring = false
            });

            await client.WaitForState();
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [UnityTest]
        public IEnumerator TokenAuthUrlWithIncorrectJsonTokenReturned_ShouldNotBeAbleToConnectAndShouldHaveError()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthUrlWithIncorrectJsonTokenReturned_ShouldNotBeAbleToConnectAndShouldHaveError();
            });
        }

        public async Task UnityTest_TokenAuthUrlWithIncorrectJsonTokenReturned_ShouldNotBeAbleToConnectAndShouldHaveError(Protocol protocol = Protocol.Json)
        {
            var ablyRest = await UnitySandbox.GetRestClient(protocol);
            var token = await ablyRest.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" });
            var settings = await _sandboxFixture.GetSettings();
            var incorrectJson = $"[{token.ToJson()}]";
            var authUrl = "http://echo.ably.io/?type=json&body=" + Uri.EscapeUriString(incorrectJson);

            var client = new AblyRealtime(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Defaults.Protocol,
                HttpRequestTimeout = new TimeSpan(0, 0, 20),
                AutomaticNetworkStateMonitoring = false
            });

            var tsc = new TaskCompletionAwaiter();
            ErrorInfo err = null;
            client.Connection.On(ConnectionEvent.Disconnected, state =>
            {
                err = state.Reason;
                tsc.SetCompleted();
            });

            var b = await tsc.Task;
            b.Should().BeTrue();
            err.Should().NotBeNull();
            err.Message.Should().StartWith("Error parsing JSON response");
            err.InnerException.Should().NotBeNull();
        }


        [UnityTest]
        public IEnumerator TokenAuthCallbackWithTokenDetailsReturned_ShouldBeAbleToPublishWithNewToken()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthCallbackWithTokenDetailsReturned_ShouldBeAbleToPublishWithNewToken();
            });
        }

        public async Task UnityTest_TokenAuthCallbackWithTokenDetailsReturned_ShouldBeAbleToPublishWithNewToken(Protocol protocol = Protocol.Json)
        {
            var settings = await _sandboxFixture.GetSettings();
            var tokenClient = await UnitySandbox.GetRestClient(protocol);
            var authCallbackClient = await UnitySandbox.GetRestClient(protocol, options =>
            {
                options.AuthCallback = tokenParams => tokenClient.Auth.RequestTokenAsync(new TokenParams { ClientId = "*" }).Convert();
                options.Environment = settings.Environment;
                options.UseBinaryProtocol = protocol == Defaults.Protocol;
            });

            var channel = authCallbackClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            await Task.Delay(1000);

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }


        [UnityTest]
        public IEnumerator TokenAuthCallbackWithTokenRequestReturned_ShouldBeAbleToGetATokenAndPublishWithNewToken()
        {
            yield return Await(async () =>
            {
                await UnityTest_TokenAuthCallbackWithTokenRequestReturned_ShouldBeAbleToGetATokenAndPublishWithNewToken();
            });
        }

        public async Task UnityTest_TokenAuthCallbackWithTokenRequestReturned_ShouldBeAbleToGetATokenAndPublishWithNewToken(Protocol protocol = Protocol.Json)
        {
            var settings = await _sandboxFixture.GetSettings();
            var tokenClient = await UnitySandbox.GetRestClient(protocol);
            var authCallbackClient = await UnitySandbox.GetRestClient(protocol, options =>
            {
                options.AuthCallback = async tokenParams => await tokenClient.Auth.CreateTokenRequestAsync(new TokenParams { ClientId = "*" });
                options.Environment = settings.Environment;
                options.UseBinaryProtocol = protocol == Defaults.Protocol;
            });

            var channel = authCallbackClient.Channels["pesisted:test"];
            await channel.PublishAsync(new Message("test", "test") { ClientId = "123" });

            var message = (await channel.HistoryAsync()).Items.First();
            message.ClientId.Should().Be("123");
            message.Data.Should().Be("test");
        }

        
        [NUnit.Framework.Property("issue", "374")]
        [UnityTest]
        public IEnumerator WhenClientTimeIsWrongAndQueryTimeSetToTrue_ShouldNotTreatTokenAsInvalid()
        {
            yield return Await(async () =>
            {
                await UnityTest_WhenClientTimeIsWrongAndQueryTimeSetToTrue_ShouldNotTreatTokenAsInvalid();
            });
        }

        public async Task UnityTest_WhenClientTimeIsWrongAndQueryTimeSetToTrue_ShouldNotTreatTokenAsInvalid(Protocol protocol = Protocol.Json)
        {
            // Our device's clock is fast. The server returns by default a token valid for an hour
            DateTimeOffset NowFunc() => DateTimeOffset.UtcNow.AddHours(2);

            var realtimeClient = await UnitySandbox.GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.NowFunc = NowFunc;
                opts.QueryTime = true;
                opts.ClientId = "clientId";
                opts.UseTokenAuth = true; // We force the token auth because further on it's not necessary when there is a key present
            });

            await realtimeClient.WaitForState(ConnectionState.Connected);
        }

        private class RSA4Helper
        {
            private AuthSandboxSpecs Specs { get; set; }

            public List<AblyRequest> Requests { get; set; }

            public RSA4Helper(AuthSandboxSpecs specs)
            {
                Requests = new List<AblyRequest>();
                Specs = specs;
            }

            public async Task<AblyRest> GetRestClientWithRequests(Protocol protocol, TokenDetails token,
                bool invalidateKey, Action<ClientOptions> optionsAction = null)
            {
                void DefaultOptionsAction(ClientOptions options)
                {
                    options.TokenDetails = token;
                    if (invalidateKey)
                    {
                        options.Key = string.Empty;
                    }
                }

                if (optionsAction == null)
                {
                    optionsAction = DefaultOptionsAction;
                }

                var restClient = await Specs.UnitySandbox.GetRestClient(protocol, optionsAction);

                // intercept http calls to demonstrate that the client did not attempt to request a new token
                var execute = restClient.ExecuteHttpRequest;
                restClient.ExecuteHttpRequest = request =>
                {
                    Requests.Add(request);
                    return execute.Invoke(request);
                };

                return restClient;
            }

            public async Task<AblyRealtime> GetRealTimeClientWithRequests(Protocol protocol, TokenDetails token,
                bool invalidateKey, Action<ClientOptions, TestEnvironmentSettings> optionsAction = null)
            {
                var restClient = await GetRestClientWithRequests(protocol, token, invalidateKey);

                // Creating a new connection
                void DefaultOptionsAction(ClientOptions options, TestEnvironmentSettings settings)
                {
                    options.TokenDetails = token;
                    if (invalidateKey)
                    {
                        options.Key = string.Empty;
                    }
                }

                if (optionsAction == null)
                {
                    optionsAction = DefaultOptionsAction;
                }

                var realtimeClient =
                    await Specs.UnitySandbox.GetRealtimeClient(protocol, optionsAction,
                        (options, device) => restClient);
                return realtimeClient;
            }

            public Task<AblyResponse> AblyResponseWith500Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "application/json", string.Empty.GetBytes())
                    {StatusCode = HttpStatusCode.InternalServerError};
                return Task.FromResult(r);
            }
        }
    }

    public static class E7Assert
    {
        public static async Task<T> ThrowsAsync<T>(Task asyncMethod) where T : Exception
        {
            return await ThrowsAsync<T>(asyncMethod, "");
        }

        public static async Task<T> ThrowsAsync<T>(Task asyncMethod, string message) where T : Exception
        {
            try
            {
                await asyncMethod; //Should throw..
            }
            catch (T e)
            {
                //Ok! Swallow the exception.
                return e;
            }
            catch (Exception e)
            {
                if (message != "")
                {
                    Assert.That(e, Is.TypeOf<T>(), message + " " + e.ToString()); //of course this fail because it goes through the first catch..
                }
                else
                {
                    Assert.That(e, Is.TypeOf<T>(), e.ToString());
                }
                return (T) e; //probably unreachable
            }
            Assert.Fail("Expected an exception of type " + typeof(T).FullName + " but no exception was thrown.");
            return null;
        }
    }
}
