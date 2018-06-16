using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("requires", "sandbox")]
    public class RealtimeSandboxSpecs : SandboxSpecs
    {
        public RealtimeSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC8")]
        public async Task WithNotConnectedClient_AuthorizeObtainsNewTokenAndConnects(Protocol protocol)
        {
            // TODO: test against all non CONNECTED states
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

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC8")]
        [Trait("spec", "RTC8a")]
        [Trait("spec", "RTC8a1")]
        [Trait("spec", "RTC8a2")]
        public async Task WithConnectedClient_AuthorizeObtainsNewTokenAndUpgradesConnection_AndShouldEmitUpdate(
            Protocol protocol)
        {
            var validClientId = "RTC8a";
            var invalidClientId = "RTC8a-incompatible-clientId";

            var client = await GetRealtimeClient(protocol, (opts, _) => { opts.ClientId = validClientId; });

            var awaiter = new TaskCompletionAwaiter();
            client.Connection.On(ConnectionEvent.Update, args => { awaiter.SetCompleted(); });

            await client.WaitForState(ConnectionState.Connected);

            var tokenDetails = await client.Auth.AuthorizeAsync(new TokenParams { ClientId = validClientId });
            tokenDetails.ClientId.Should().Be(validClientId);

            client.Connection.State.Should().Be(ConnectionState.Connected);
            client.RestClient.AblyAuth.CurrentToken.Should().Be(tokenDetails);
            var didUpdate = await awaiter.Task;
            client.Connection.State.Should().Be(ConnectionState.Connected);
            didUpdate.Should().BeTrue(
                "the AUTH message should trigger CONNECTED response from the server that causes an UPDATE to be emitted.");

            client.Connection.On(args =>
            {
                if (args.Current != ConnectionState.Failed)
                {
                    Assert.True(false, "$unexpected state '{args.Current}'");
                }
            });

            await client.Auth.AuthorizeAsync(new TokenParams { ClientId = invalidClientId });
            await client.WaitForState(ConnectionState.Failed);
            client.Connection.State.Should().Be(ConnectionState.Failed);
        }
    }
}