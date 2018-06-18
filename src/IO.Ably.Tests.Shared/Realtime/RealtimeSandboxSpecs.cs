using System;
using System.Collections.Generic;
using System.Net;
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
        [Trait("spec", "RTC8a3")]
        public async Task WithConnectedClient_AuthorizeObtainsNewTokenAndUpgradesConnection_AndShouldEmitUpdate(
            Protocol protocol)
        {
            var validClientId1 = "RTC8";
            var invalidClientId = "RTC8-incompatible-clientId";

            // For a realtime client, Auth#authorize instructs the library to obtain
            // a token using the provided tokenParams and authOptions and upgrade
            // the current connection to use that token
            var client = await GetRealtimeClient(protocol, (opts, _) => { opts.ClientId = validClientId1; });

            var awaiter = new TaskCompletionAwaiter();
            client.Connection.On(ConnectionEvent.Update, args => { awaiter.SetCompleted(); });
            await client.WaitForState(ConnectionState.Connected);

            var tokenDetails = await client.Auth.AuthorizeAsync(new TokenParams {ClientId = validClientId1});
            tokenDetails.ClientId.Should().Be(validClientId1);
            client.Connection.State.Should().Be(ConnectionState.Connected);
            client.RestClient.AblyAuth.CurrentToken.Should().Be(tokenDetails);
            var didUpdate = await awaiter.Task;
            client.Connection.State.Should().Be(ConnectionState.Connected);
            didUpdate.Should().BeTrue(
                "the AUTH message should trigger a CONNECTED response from the server that causes an UPDATE to be emitted.");

            client.Connection.On(args =>
            {
                if (args.Current != ConnectionState.Failed)
                {
                    Assert.True(false, $"unexpected state '{args.Current}'");
                }
            });

            // AuthorizeAsync will not return until either a CONNECTED or ERROR response
            // (or timeout) is seen from Ably, so we do not need to use WaitForState() here
            await client.Auth.AuthorizeAsync(new TokenParams {ClientId = invalidClientId});
            client.Connection.State.Should().Be(ConnectionState.Failed);
            client.Close();

            // if not currently connected, to connects with the token.
            var client2 = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.TokenDetails = tokenDetails;
            });
            await client2.Auth.AuthorizeAsync();
            await client2.WaitForState(ConnectionState.Connected);
            client2.Connection.State.Should().Be(ConnectionState.Connected);
            client2.Close();

            // internally AblyAuth.AuthorizeCompleted is used to indicate when an Authorize call is finished
            // AuthorizeCompleted should timeout if no valid response (CONNECTED or ERROR) is received from Ably
            var auth = new AblyAuth(client.Options, client.RestClient);
            auth.Options.RealtimeRequestTimeout = TimeSpan.FromSeconds(1);
            var authEventArgs = new AblyAuthUpdatedEventArgs();
            try
            {
                var result = await auth.AuthorizeCompleted(authEventArgs);
                result.Should().BeFalse();
                throw new Exception("AuthorizeCompleted did not raise an exception.");
            }
            catch (AblyException e)
            {
                e.Should().BeOfType<AblyException>();
                e.ErrorInfo.Code.Should().Be(40140);
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC8a1")]
        public async Task WithConnectedClient_WhenUpgradingCapabilities_ConnectionShouldNotBeImpaired(Protocol protocol)
        {
            var clientId = "RTC8a1".AddRandomSuffix();
            var capability = new Capability();
            capability.AddResource("foo").AllowPublish();

            var restClient = await GetRestClient(protocol);
            var tokenDetails = await restClient.Auth.RequestTokenAsync(new TokenParams
            {
                ClientId = clientId,
                Capability = capability
            });

            var realtime = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.Token = tokenDetails.Token;
            });
            await realtime.WaitForState();

            // upgrade of capabilities without any loss of continuity or connectivity
            realtime.Connection.Once(ConnectionEvent.Disconnected, change => throw new Exception("should not disconnect"));

            var fooChannel = realtime.Channels.Get("foo");
            var barChannel = realtime.Channels.Get("bar");

            var fooSuccessAWaiter = new TaskCompletionAwaiter(5000);
            fooChannel.Publish("test", "should-not-fail", (b, info) =>
            {
                // foo should succeed
                b.Should().BeTrue();
                info.Should().BeNull();
                fooSuccessAWaiter.SetCompleted();
            });
            Assert.True(await fooSuccessAWaiter.Task);

            var barFailAwaiter = new TaskCompletionAwaiter(5000);
            barChannel.Publish("test", "should-fail", (b, info) =>
            {
                // bar should fail
                b.Should().BeFalse();
                info.Code.Should().Be(40160);
                barFailAwaiter.SetCompleted();
            });
            Assert.True(await barFailAwaiter.Task);

            // upgrade bar
            capability = new Capability();
            capability.AddResource("bar").AllowPublish();
            await realtime.Auth.AuthorizeAsync(new TokenParams
            {
                Capability = capability,
                ClientId = clientId
            });
            realtime.Connection.State.Should().Be(ConnectionState.Connected);
            var barSuccessAwaiter = new TaskCompletionAwaiter(5000);
            barChannel.Attach((b2, info2) =>
            {
                b2.Should().BeTrue();
                barChannel.Publish("test", "should-succeed", (b, info) =>
                {
                    b.Should().BeTrue();
                    info.Should().BeNull();
                    barSuccessAwaiter.SetCompleted();
                });
            });

            Assert.True(await barSuccessAwaiter.Task);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC8a1")]
        public async Task WithConnectedClient_WhenDowngradingCapabilities_ChannelShouldBecomeFailed(Protocol protocol)
        {
            var clientId = "RTC8a1-downgrade".AddRandomSuffix();
            var channelName = "RTC8a1-downgrade-channel".AddRandomSuffix();
            var wrongChannelName = "wrong".AddRandomSuffix();
            var capability = new Capability();
            capability.AddResource(channelName).AllowAll();

            var restClient = await GetRestClient(protocol);
            var tokenDetails = await restClient.Auth.RequestTokenAsync(new TokenParams
            {
                ClientId = clientId,
                Capability = capability
            });

            var realtime = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.Token = tokenDetails.Token;
            });
            await realtime.WaitForState(ConnectionState.Connected);

            realtime.Connection.Once(ConnectionEvent.Disconnected, change => throw new Exception("Should not require a disconnect"));
            var channel = realtime.Channels.Get(channelName);
            var awaiter1 = new TaskCompletionAwaiter(10000);
            channel.Publish("test", "should-not-fail", (b, info) =>
            {
                b.Should().BeTrue();
                info.Should().BeNull();
                awaiter1.SetCompleted();
            });
            Assert.True(await awaiter1.Task);
            channel.State.Should().Be(ChannelState.Attached);

            // channel should fail fast, allow 2000ms
            var channelFailedAwaiter = new TaskCompletionAwaiter(2000);
            channel.Attach(async (success, info2) =>
            {
                success.Should().BeTrue();

                // downgrade
                capability = new Capability();
                capability.AddResource(wrongChannelName).AllowSubscribe();
                var newToken = await realtime.Auth.AuthorizeAsync(new TokenParams
                {
                    Capability = capability,
                    ClientId = clientId
                });
                newToken.Should().NotBeNull();
                channel.Once(ChannelState.Failed, state =>
                {
                    state.Error.Code.Should().Be(40160);
                    state.Error.Message.Should().Contain("Channel denied access");
                    channelFailedAwaiter.SetCompleted();
                });
            });

            var channelFailed = await channelFailedAwaiter.Task;
            channelFailed.Should().BeTrue("channel should have failed");
        }
    }
}
