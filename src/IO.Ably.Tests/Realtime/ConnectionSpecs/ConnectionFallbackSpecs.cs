using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Microsoft.Build.Utilities;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace IO.Ably.Tests.Realtime.ConnectionSpecs
{
    [Trait("spec", "RTN17")]
    public class ConnectionFallbackSpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN17b")]
        public async Task WithCustomHostAndError_ConnectionGoesStraightToFailedInsteadOfDisconnected()
        {
            var client = GetConnectedClient(opts => opts.RealtimeHost = "test.com");

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() {statusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionStateType.Failed);
        }

        [Fact]
        [Trait("spec", "RTN17b")]
        public async Task WithCustomPortAndError_ConnectionGoesStraightToFailedInsteadOfDisconnected()
        {
            var client = GetConnectedClient(opts => opts.Port = 100);

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionStateType.Failed);
        }

        [Fact]
        [Trait("spec", "RTN17b")]
        public async Task WithCustomEnvironmentAndError_ConnectionGoesStraightToFailedInsteadOfDisconnected()
        {
            var client = GetConnectedClient(opts => opts.Environment = AblyEnvironment.Sandbox);

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionStateType.Failed);
        }

        [Fact]
        [Trait("spec", "RTN17a")]
        public async Task WhenPreviousAttemptFailed_ShouldGoToDefaultHostFirst()
        {
            var client = GetConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.CurrentState);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });
            await client.ConnectionManager.SetState(new ConnectionFailedState(client.ConnectionManager, ErrorInfo.ReasonFailed));
            client.Connect();
            LastCreatedTransport.Parameters.Host.Should().Be(Defaults.RealtimeHost);
        }

        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WithDefaultHostAndRecoverableError_ConnectionGoesToDisconnectedInsteadOfFailedAndRetryInstantly()
        {
            var client = GetConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.CurrentState);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            states.First().Should().Be(ConnectionStateType.Disconnected);
            states.Last().Should().Be(ConnectionStateType.Connecting);
        }

        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WithDefaultHostAndRecoverableError_ShouldRetryInstantlyOnFallbackHost()
        {
            var client = GetConnectedClient();

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.CurrentState);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            states.First().Should().Be(ConnectionStateType.Disconnected);
            states.Last().Should().Be(ConnectionStateType.Connecting);

            Defaults.FallbackHosts.Should().Contain(LastCreatedTransport.Parameters.Host);
            client.Connection.Host.Should().Be(LastCreatedTransport.Parameters.Host);
        }

        [Fact]
        [Trait("spec", "RTN17e")]
        public async Task WithFallbackHost_ShouldMakeRestRequestsOnSameHost()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("[12345678]") };
            var handler = new FakeHttpMessageHandler(response);
            var client = new AblyRealtime(new ClientOptions(ValidKey)
            {
                UseBinaryProtocol = false,
                UseSyncForTesting = true,
                SkipInternetCheck = true,
                TransportFactory = _fakeTransportFactory
            });
            client.RestClient.HttpClient.CreateInternalHttpClient(TimeSpan.FromSeconds(10), handler);
            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                connectionDetails = new ConnectionDetailsMessage() { connectionKey = "connectionKey" },
                connectionId = "1",
                connectionSerial = 100
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            await client.Time();
            handler.Requests.Last().RequestUri.ToString().Should().Contain(client.Connection.Host);
        }

        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WhileInDisconnectedStateLoop_ShouldRetryWithMultipleHosts()
        {
            var client = GetConnectedClient(opts => opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10));
               
            List<ConnectionStateType> states = new List<ConnectionStateType>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.CurrentState);
            };

            List<string> retryHosts = new List<string>();

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            for (int i = 0; i < 5; i++)
            {
                if (client.Connection.State == ConnectionStateType.Connecting)
                {
                    retryHosts.Add(LastCreatedTransport.Parameters.Host);
                }
                else
                {
                    await Task.Delay(50); //wait just enough for the disconnect timer to kick in
                    retryHosts.Add(LastCreatedTransport.Parameters.Host);
                }

                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
                {
                    error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
                });
            }

            retryHosts.Should().BeEquivalentTo(Defaults.FallbackHosts);
            Output.WriteLine(string.Join(",", retryHosts));
        }

        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WhenItMovesFromDisconnectedToSuspended_ShouldTryDefaultHostAgain()
        {
            var client = GetConnectedClient(opts =>
            {
                opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10);
                opts.SuspendedRetryTimeout = TimeSpan.FromMilliseconds(10);
            });

            List<ConnectionStateType> states = new List<ConnectionStateType>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.CurrentState);
            };

            List<string> retryHosts = new List<string>();

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            for (int i = 0; i < 5; i++)
            {
                Now = Now.AddSeconds(60);
                if (client.Connection.State == ConnectionStateType.Connecting)
                {
                    retryHosts.Add(LastCreatedTransport.Parameters.Host);
                }
                else
                {
                    await Task.Delay(50); //wait just enough for the disconnect timer to kick in
                    retryHosts.Add(LastCreatedTransport.Parameters.Host);
                }

                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
                {
                    error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
                });
            }

            Output.WriteLine(string.Join(",", client.ConnectionManager.AttemptsInfo.Attempts.SelectMany(x => x.FailedStates).Select(x => x.State)));
            Output.WriteLine(string.Join(",", retryHosts));

            retryHosts.Should().Contain("realtime.ably.io");
            retryHosts.Count(x => x == "realtime.ably.io").Should().Be(1);
        }


        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WithDefaultHostAndRecoverableError_WhenInternetDown_GoesStraightToFailed()
        {
            var client = GetConnectedClient(null, request =>
            {
                if (request.Url == Defaults.InternetCheckURL)
                {
                    return "no".ToAblyResponse();
                }
                return DefaultResponse.ToTask();
            });
            client.Options.SkipInternetCheck = false;

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                error = new ErrorInfo() { statusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionStateType.Failed);
        }

        public ConnectionFallbackSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}