using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("SandBox Connection")]
    [Trait("type", "integration")]
    public class ConnectionSandboxTransportSideEffectsSpecs : SandboxSpecs
    {
        /*
         * (RTN19b) If there are any pending channels i.e. in the ATTACHING or DETACHING state,
         * the respective ATTACH or DETACH message should be resent to Ably
         */
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed(Protocol protocol)
        {
            var channelName = "test-channel".AddRandomSuffix();
            var sentMessages = new List<ProtocolMessage>();
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory()
                {
                    OnMessageSent = sentMessages.Add
                };
            });

            await client.WaitForState(ConnectionState.Connected);

            var transport = client.GetTestTransport();
            transport.MessageSent = sentMessages.Add;

            var channel = client.Channels.Get(channelName);
            channel.Once(ChannelEvent.Attaching, change =>
            {
                transport.Close(false);
            });
            channel.Attach();
            await channel.WaitForState(ChannelState.Attaching);
            bool didDisconnect = false;
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                didDisconnect = true;
                sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach).Should().Be(1);
            });

            await client.WaitForState(ConnectionState.Disconnected);
            await client.WaitForState(ConnectionState.Connecting);
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.State.Should().Be(ConnectionState.Connected);
            didDisconnect.Should().BeTrue();

            await channel.WaitForState(ChannelState.Attached);

            var attachCount = sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach);
            attachCount.Should().Be(2);

            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed(Protocol protocol)
        {
            var channelName = "test-channel".AddRandomSuffix();
            var sentMessages = new List<ProtocolMessage>();
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory()
                {
                    OnMessageSent = sentMessages.Add
                };
            });

            await client.WaitForState(ConnectionState.Connected);

            var transport = client.GetTestTransport();
            transport.MessageSent = sentMessages.Add;

            var channel = client.Channels.Get(channelName);
            channel.Once(ChannelEvent.Detaching, change =>
            {
                transport.Close(false);
            });
            channel.Attach();
            await channel.WaitForState(ChannelState.Attached);
            channel.Detach();
            await channel.WaitForState(ChannelState.Detaching);
            bool didDisconnect = false;
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                didDisconnect = true;
                sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach).Should().Be(1);
            });

            await client.WaitForState(ConnectionState.Disconnected);
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.State.Should().Be(ConnectionState.Connected);
            didDisconnect.Should().BeTrue();

            await channel.WaitForState(ChannelState.Detached);

            var detatchCount = sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Detach);
            detatchCount.Should().Be(2);
        }

        public ConnectionSandboxTransportSideEffectsSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base((AblySandboxFixture) fixture, (ITestOutputHelper) output)
        {
        }
    }
}