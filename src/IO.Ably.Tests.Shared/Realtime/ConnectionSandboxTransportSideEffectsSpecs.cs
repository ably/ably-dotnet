using System;
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
                var optionsTransportFactory = new TestTransportFactory()
                {
                    OnMessageSent = sentMessages.Add,
                };
                options.TransportFactory = optionsTransportFactory;
            });

            await client.WaitForState(ConnectionState.Connected);

            var transport = client.GetTestTransport();

            var channel = client.Channels.Get(channelName);
            channel.Attach();
            await channel.WaitForState(ChannelState.Attaching);
            await client.ProcessCommands();
            transport.Close(false);

            bool didDisconnect = false;
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                didDisconnect = true;
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
            int detachMessageCount = 0;
            AblyRealtime client = null;
            var channelName = "test-channel".AddRandomSuffix();

            client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory()
                {
                    OnMessageSent = OnMessageSent,
                };
            });

            void OnMessageSent(ProtocolMessage message)
            {
                if (message.Action == ProtocolMessage.MessageAction.Detach)
                {
                    detachMessageCount++;
                    if (detachMessageCount == 1)
                    {
                        client.GetTestTransport().Close(suppressClosedEvent: false);
                    }
                }
            }

            await client.WaitForState(ConnectionState.Connected);

            var channel = client.Channels.Get(channelName);
            await channel.AttachAsync();
            channel.Detach();
            await channel.WaitForState(ChannelState.Detaching);

            await client.WaitForState(ConnectionState.Disconnected);
            channel.State.Should().Be(ChannelState.Detaching);
            await client.WaitForState(ConnectionState.Connected);

            await channel.WaitForState(ChannelState.Detached, TimeSpan.FromSeconds(10));
        }

        public ConnectionSandboxTransportSideEffectsSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }
}
