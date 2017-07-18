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
                Error = new ErrorInfo() {StatusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionState.Failed);
        }

        [Fact]
        [Trait("spec", "RTN17b")]
        public async Task WithCustomPortAndError_ConnectionGoesStraightToFailedInsteadOfDisconnected()
        {
            var client = GetConnectedClient(opts => opts.Port = 100);

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionState.Failed);
        }

        [Fact]
        [Trait("spec", "RTN17b")]
        public async Task WithCustomEnvironmentAndError_ConnectionGoesStraightToFailedInsteadOfDisconnected()
        {
            var client = GetConnectedClient(opts => opts.Environment = "sandbox");

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionState.Failed);
        }

        [Fact]
        [Trait("spec", "RTN17a")]
        public async Task WhenPreviousAttemptFailed_ShouldGoToDefaultHostFirst()
        {
            var client = GetConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.Current);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
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

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.Current);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            states.First().Should().Be(ConnectionState.Disconnected);
            states.Last().Should().Be(ConnectionState.Connecting);
        }

        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WithDefaultHostAndRecoverableError_ShouldRetryInstantlyOnFallbackHost()
        {
            var client = GetConnectedClient();

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.Current);
            };

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            states.First().Should().Be(ConnectionState.Disconnected);
            states.Last().Should().Be(ConnectionState.Connecting);

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
                ConnectionDetails = new ConnectionDetails() { ConnectionKey = "connectionKey" },
                ConnectionId = "1",
                ConnectionSerial = 100
            });

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            await client.TimeAsync();
            handler.Requests.Last().RequestUri.ToString().Should().Contain(client.Connection.Host);
        }

        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WhileInDisconnectedStateLoop_ShouldRetryWithMultipleHosts()
        {
            var client = GetConnectedClient(opts => opts.DisconnectedRetryTimeout = TimeSpan.FromMilliseconds(10));
               
            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.Current);
            };

            List<string> retryHosts = new List<string>();

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            for (int i = 0; i < 5; i++)
            {
                if (client.Connection.State == ConnectionState.Connecting)
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
                    Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
                });
            }

            retryHosts.Count.Should().BeGreaterOrEqualTo(3);
            retryHosts.Distinct().Count().Should().BeGreaterOrEqualTo(3);
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

            List<ConnectionState> states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                states.Add(args.Current);
            };

            List<string> retryHosts = new List<string>();

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            for (int i = 0; i < 5; i++)
            {
                Now = Now.AddSeconds(60);
                if (client.Connection.State == ConnectionState.Connecting)
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
                    Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
                });
            }

            retryHosts.Should().Contain("realtime.ably.io");
            retryHosts.Count(x => x == "realtime.ably.io").Should().Be(1);
        }


        [Fact]
        [Trait("spec", "RTN17c")]
        public async Task WithDefaultHostAndRecoverableError_WhenInternetDown_GoesStraightToFailed()
        {
            var client = GetConnectedClient(null, request =>
            {
                if (request.Url == Defaults.InternetCheckUrl)
                {
                    return "no".ToAblyResponse();
                }
                return DefaultResponse.ToTask();
            });
            client.Options.SkipInternetCheck = false;

            await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error)
            {
                Error = new ErrorInfo() { StatusCode = HttpStatusCode.GatewayTimeout }
            });

            client.Connection.State.Should().Be(ConnectionState.Failed);
        }

        public ConnectionFallbackSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}