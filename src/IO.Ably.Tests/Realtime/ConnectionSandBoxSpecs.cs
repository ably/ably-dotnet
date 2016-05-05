using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Auth;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class ConnectionSandBoxSpecs : SandboxSpecs
    {
        private Task WaitForState(AblyRealtime realtime, ConnectionStateType awaitedState = ConnectionStateType.Connected, TimeSpan? waitSpan = null)
        {
            
            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
                return connectionAwaiter.Wait(waitSpan.Value);
            return connectionAwaiter.Wait();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN6")]
        public async Task WithAutoConnectTrue_ShouldConnectToAblyInTheBackground(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await WaitForState(client);

            client.Connection.State.Should().Be(ConnectionStateType.Connected);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN4b")]
        [Trait("spec", "RTN4d")]
        [Trait("spec", "RTN4e")]
        [Trait("spec", "RTN11a")]
        public async Task ANewConnectionShouldRaiseConnectingAndConnectedEvents(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, opts => opts.AutoConnect = false);
            var states = new List<ConnectionStateType>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                states.Add(args.CurrentState);
            };

            client.Connect();

            await WaitForState(client);

            states.Should().BeEquivalentTo(new[] { ConnectionStateType.Connecting, ConnectionStateType.Connected });
            client.Connection.State.Should().Be(ConnectionStateType.Connected);
        }

        [Theory]
        [ProtocolData]
        public async Task WhenClosingAConnection_ItShouldRaiseClosingAndClosedEvents(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            //Start collecting events after the connection is open
            await WaitForState(client);

            var states = new List<ConnectionStateType>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                states.Add(args.CurrentState);
            };
            client.Close();

            await WaitForState(client, ConnectionStateType.Closed, TimeSpan.FromSeconds(5));

            states.Should().BeEquivalentTo(new[] { ConnectionStateType.Closing, ConnectionStateType.Closed });
            client.Connection.State.Should().Be(ConnectionStateType.Closed);
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

            //Wait for the clients to connect
            await Task.Delay(TimeSpan.FromSeconds(3));

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

            var result = await client.Connection.Ping();

            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14a")]
        public async Task WithInvalidApiKey_ShouldSetToFailedStateAndAddErrorMessageToEmittedState(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, options =>
            {
                options.AutoConnect = false;
                options.Key = "baba.bobo:bosh";
            });

            ErrorInfo error = null;
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                error = args.Reason;
            };

            client.Connect();

            await WaitForState(client, ConnectionStateType.Failed);

            error.Should().NotBeNull();
            client.Connection.Reason.Should().BeSameAs(error);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN14b")]
        public async Task WithExpiredRenewableToken_ShouldAutomaticallyRenewTokenAndNoErrorShouldBeEmitted(
            Protocol protocol)
        {
            var restClient = await GetRestClient(protocol);
            var invalidToken = await restClient.Auth.RequestToken();

            //Use an old token which will result in 40143 Unrecognised token
            invalidToken.Token = invalidToken.Token.Split('.')[0] + ".DOcRVPgv1Wf1-YGgJFjyk2PNOGl_DFL7aCDzEPju8TYHorfxHHVoNoDGz5fKRW0UxePiVjD1EVEW0ZiknIK8u3S5p1FBq5Rtw_I7OX7fW8U4sGxJjAfMS_fTcXFdvouTQ";

            var realtimeClient = await GetRealtimeClient(protocol, options =>
            {
                options.TokenDetails = invalidToken;
                options.AutoConnect = false;
            });

            ErrorInfo error = null;
            realtimeClient.Connection.ConnectionStateChanged += (o, args) =>
            {
                error = args.Reason;
            };

            realtimeClient.Connect();

            await WaitForState(realtimeClient, ConnectionStateType.Connected, TimeSpan.FromSeconds(10));

            realtimeClient.Auth.CurrentToken.Expires.Should().BeAfter(Config.Now(), "The token should be valid and expire in the future.");
            error.Should().BeNull("No error should be raised!");
        }

        public ConnectionSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }
}