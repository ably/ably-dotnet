using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Shared.Realtime
{
    [Trait("requires", "sandbox")]
    public class RealtimeSandbox : SandboxSpecs
    {
        public RealtimeSandbox(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC8")]
        public async Task WithConnectedClient_AuthorizeObtainsNewTokenAndUpgradesConnection(Protocol protocol)
        {
            // For a realtime client, Auth#authorize instructs the library to obtain a token using the provided tokenParams and authOptions and upgrade the current connection to use that token; or if not currently connected, to connect with the token.
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.ClientId = "RTC8";
            });

            client.Connect();

            await client.WaitForState(ConnectionState.Connected);

            var tokenDetails = await client.Auth.AuthorizeAsync(new TokenParams { ClientId = "RTC8" });
            tokenDetails.ClientId.Should().Be("RTC8");

            client.Connection.State.Should().Be(ConnectionState.Connected);
            client.RestClient.AblyAuth.CurrentToken.Should().Be(tokenDetails);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC8")]
        public async Task WithNotConnectedClient_AuthorizeObtainsNewTokenAndConnects(Protocol protocol)
        {
            // For a realtime client, Auth#authorize instructs the library to obtain a token using the provided tokenParams and authOptions and upgrade the current connection to use that token; or if not currently connected, to connect with the token.
            var client = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.ClientId = "RTC8";
            });

            // show that the connection is not connected
            client.Connection.State.Should().Be(ConnectionState.Initialized);

            var tokenDetails = await client.Auth.AuthorizeAsync();
            tokenDetails.ClientId.Should().Be("RTC8");

            await client.WaitForState(ConnectionState.Connecting);
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.State.Should().Be(ConnectionState.Connected);
        }
    }
}
