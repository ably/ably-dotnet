using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using NUnit.Framework;

namespace IO.Ably.TestHelpers.Unity
{
    [TestFixture]
    public class AuthSandboxSpecs
    {
        [OneTimeSetUp]
        public void OneTimeInit()
        {
            UnitySandbox = new UnitySandboxSpecs(new AblySandboxFixture());
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            UnitySandbox.Dispose();
        }

        public UnitySandboxSpecs UnitySandbox { get; set; }

        private class RSA4Helper
        {
            private AuthSandboxSpecs Specs { get; set; }

            public List<AblyRequest> Requests { get; set; }

            public RSA4Helper(AuthSandboxSpecs specs)
            {
                Requests = new List<AblyRequest>();
                Specs = specs;
            }

            public async Task<AblyRest> GetRestClientWithRequests(Protocol protocol, TokenDetails token, bool invalidateKey, Action<ClientOptions> optionsAction = null)
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

            public async Task<AblyRealtime> GetRealTimeClientWithRequests(Protocol protocol, TokenDetails token, bool invalidateKey, Action<ClientOptions, TestEnvironmentSettings> optionsAction = null)
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

                var realtimeClient = await Specs.UnitySandbox.GetRealtimeClient(protocol, optionsAction, (options, device) => restClient);
                return realtimeClient;
            }

            public Task<AblyResponse> AblyResponseWith500Status(AblyRequest request)
            {
                Requests.Add(request);
                var r = new AblyResponse(string.Empty, "application/json", string.Empty.GetBytes()) { StatusCode = HttpStatusCode.InternalServerError };
                return Task.FromResult(r);
            }
        }

        [Test]
        public async Task RealtimeClient_ConnectedWithExpiringToken_WhenTokenExpired_ShouldNotRetryAndHaveError(Protocol protocol)
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
    }
}
