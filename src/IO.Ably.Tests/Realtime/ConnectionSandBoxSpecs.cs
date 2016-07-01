using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("SandBox Connection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandBoxSpecs : SandboxSpecs
    {
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN6")]
        public async Task WithAutoConnectTrue_ShouldConnectToAblyInTheBackground(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await WaitForState(client);



            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN4b")]
        [Trait("spec", "RTN4d")]
        [Trait("spec", "RTN4e")]
        [Trait("spec", "RTN11a")]
        public async Task ANewConnectionShouldRaiseConnectingAndConnectedEvents(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false);
            var states = new List<ConnectionState>();
            client.Connection.On((args) =>
            {
                args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                states.Add(args.Current);
            });

            client.Connect();

            await WaitForState(client);
            await Task.Delay(10);

            states.Should().BeEquivalentTo(new[] { ConnectionState.Connecting, ConnectionState.Connected });
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Theory]
        [ProtocolData]
        public async Task WhenClosingAConnection_ItShouldRaiseClosingAndClosedEvents(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            //Start collecting events after the connection is open
            await WaitForState(client);

            var states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                states.Add(args.Current);
            };

            client.Close();

            await WaitForState(client, ConnectionState.Closed, TimeSpan.FromSeconds(5));

            states.Should().BeEquivalentTo(new[] { ConnectionState.Closing, ConnectionState.Closed });
            client.Connection.State.Should().Be(ConnectionState.Closed);
        }

        [Theory]
        [ProtocolData]
        public async Task ShouldSaveConnectionStateTtlToConnectionObject(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            await WaitForState(client);

            client.Connection.ConnectionStateTtl.Should().NotBe(Defaults.ConnectionStateTtl);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN8b")]
        public async Task WithMultipleClients_ShouldHaveUniqueConnectionIdsProvidedByAbly(Protocol protocol)
        {
            var clients = new[]
            {
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol)
            };

            int count = 0;

            await Task.Delay(5000);

            var distinctConnectionIds = clients.Select(x => x.Connection.Id).Distinct();
            distinctConnectionIds.Should().HaveCount(3);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN9b")]
        public async Task WithMultipleClients_ShouldHaveUniqueConnectionKeysProvidedByAbly(Protocol protocol)
        {
            var clients = new[]
            {
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol),
                await GetRealtimeClient(protocol)
            };

            //Wait for the clients to connect
            await Task.Delay(TimeSpan.FromSeconds(3));

            var distinctConnectionIds = clients.Select(x => x.Connection.Key).Distinct();
            distinctConnectionIds.Should().HaveCount(3);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN13a")]
        public async Task WithConnectedClient_PingShouldReturnServiceTime(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            await WaitForState(client);

            var result = await client.Connection.PingAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14a")]
        public async Task WithInvalidApiKey_ShouldSetToFailedStateAndAddErrorMessageToEmittedState(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.Key = "baba.bobo:bosh";
            });

            ErrorInfo error = null;
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                error = args.Reason;
            };

            client.Connect();

            await WaitForState(client, ConnectionState.Failed);

            error.Should().NotBeNull();
            client.Connection.ErrorReason.Should().BeSameAs(error);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14b")]
        public async Task WithExpiredRenewableToken_ShouldAutomaticallyRenewTokenAndNoErrorShouldBeEmitted(Protocol protocol)
        {
            var restClient = await GetRestClient(protocol);
            var invalidToken = await restClient.Auth.RequestTokenAsync();

            //Use an old token which will result in 40143 Unrecognised token
            invalidToken.Token = invalidToken.Token.Split('.')[0] + ".DOcRVPgv1Wf1-YGgJFjyk2PNOGl_DFL7aCDzEPju8TYHorfxHHVoNoDGz5fKRW0UxePiVjD1EVEW0ZiknIK8u3S5p1FBq5Rtw_I7OX7fW8U4sGxJjAfMS_fTcXFdvouTQ";

            var realtimeClient = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.TokenDetails = invalidToken;
                options.AutoConnect = false;
            });

            ErrorInfo error = null;
            realtimeClient.Connection.InternalStateChanged += (o, args) =>
            {
                error = args.Reason;
            };

            realtimeClient.Connect();

            await WaitForState(realtimeClient, ConnectionState.Connected, TimeSpan.FromSeconds(15));

            realtimeClient.RestClient.AblyAuth.CurrentToken.Expires.Should().BeAfter(Config.Now(), "The token should be valid and expire in the future.");
            error.Should().BeNull("No error should be raised!");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14c")]
        public async Task ShouldFailIfConnectionIsNotEstablishedWithInDefaultTimeout(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, _) =>
            {
                options.RealtimeHost = "localhost";
                options.AutoConnect = false;
                options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(500);
            });

            client.Connect();

            await WaitForState(client, ConnectionState.Disconnected);
            client.Connection.ErrorReason.Should().NotBeNull();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN15e")]
        public async Task ShouldUpdateConnectionKeyWhenConnectionIsResumed(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            await WaitForState(client, ConnectionState.Connected);
            var initialConnectionKey = client.Connection.Key;
            var initialConnectionId = client.Connection.Id;
            client.ConnectionManager.Transport.Close(false);
            await WaitForState(client, ConnectionState.Disconnected);
            await WaitForState(client, ConnectionState.Connected, TimeSpan.FromSeconds(10));
            client.Connection.Id.Should().Be(initialConnectionId);
            client.Connection.Key.Should().NotBe(initialConnectionKey);
        }

        [Theory]
        [ProtocolData]
        public async Task WithAuthUrlShouldGetTokenFromUrl(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var token = await client.Auth.RequestTokenAsync(new TokenParams() { ClientId = "*" });
            var settings = await Fixture.GetSettings();
            var authUrl = "http://echo.ably.io/?type=text&body=" + token.Token;

            var authUrlClient = new AblyRealtime(new ClientOptions
            {
                AuthUrl = new Uri(authUrl),
                Environment = settings.Environment,
                UseBinaryProtocol = protocol == Protocol.MsgPack
            });

            await WaitForState(authUrlClient, waitSpan: TimeSpan.FromSeconds(5));
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN16d")]
        public async Task WhenRecoveringConnection_ShouldHaveSameConnectionIdButDifferentKey(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await WaitForState(client, ConnectionState.Connected);
            var id = client.Connection.Id;
            var key = client.Connection.Key;

            var recoveryClient = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.Recover = client.Connection.RecoveryKey;
                opts.AutoConnect = false;
            });

            //Kill the transport
            client.ConnectionManager.Transport.Close(true);

            recoveryClient.Connect();
            await WaitForState(recoveryClient, ConnectionState.Connected, TimeSpan.FromSeconds(5));

            recoveryClient.Connection.Id.Should().Be(id);
            recoveryClient.Connection.Key.Should().NotBe(key);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN16e")]
        public async Task WithDummyRecoverData_ShouldConnectAndSetAReasonOnTheConnection(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.Recover = "c17a8!WeXvJum2pbuVYZtF-1b63c17a8:-1";
                opts.AutoConnect = false;
            });
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                if (args.Current == ConnectionState.Connected)
                {
                    args.Reason.Should().NotBeNull();
                }
            };
            client.Connect();

            await WaitForState(client, ConnectionState.Connected, TimeSpan.FromSeconds(10));
            client.Connection.ErrorReason.code.Should().Be(80008);
        }

        [Theory]
        [ProtocolData]
        [Trait("issue", "65")]
        public async Task WithShortlivedToken_ShouldRenewTokenMoreThanOnce(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
            });
            
            var stateChanges = new List<ConnectionState>();
            var errors = new List<ErrorInfo>();

            client.Connection.InternalStateChanged += (sender, args) =>
            {
                stateChanges.Add(args.Current);
                errors.Add(args.Reason);
            };

            await client.Auth.AuthoriseAsync(new TokenParams() { Ttl = TimeSpan.FromSeconds(10) });
            var channel = client.Channels.Get("shortToken_test" + protocol);
            int count = 0;
            while (true)
            {
                count++;
                channel.Publish("test", "test");
                await Task.Delay(3000);
                if (count == 10)
                    break;
            }

            stateChanges.Count(x => x == ConnectionState.Connected).Should().BeGreaterThan(2);
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        public ConnectionSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }
}