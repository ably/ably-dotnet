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
        [Theory(Skip = "Intermittently fails")]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        public async Task WithChannelInAttachingState_WhenTransportIsDisconnected_ShouldResendAttachMessageOnConnectionResumed(Protocol protocol)
        {
            var channelName = "test-channel".AddRandomSuffix();
            var sentMessages = new List<ProtocolMessage>();

            int attachMessageCount = 0;

            AblyRealtime client = null;
            client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory
                {
                    OnMessageSent = OnMessageSent,
                };
            });

            void OnMessageSent(ProtocolMessage message)
            {
                sentMessages.Add(message);
                if (message.Action == ProtocolMessage.MessageAction.Attach)
                {
                    if (attachMessageCount == 0)
                    {
                        attachMessageCount++;
                        client.GetTestTransport().Close(suppressClosedEvent: false);
                    }
                }
            }

            bool didDisconnect = false;
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                didDisconnect = true;
            });

            await client.WaitForState(ConnectionState.Connected);

            var channel = client.Channels.Get(channelName);
            channel.Attach();

            await channel.WaitForState(ChannelState.Attaching);

            await client.WaitForState(ConnectionState.Disconnected);
            await client.WaitForState(ConnectionState.Connecting);
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.State.Should().Be(ConnectionState.Connected);
            didDisconnect.Should().BeTrue();

            await channel.WaitForAttachedState();

            var attachCount = sentMessages.Count(x => x.Channel == channelName && x.Action == ProtocolMessage.MessageAction.Attach);
            attachCount.Should().Be(2);

            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19b")]
        [Trait("intermittent", "true")] // I think the logic behind resending the detach message has an issue
        public async Task WithChannelInDetachingState_WhenTransportIsDisconnected_ShouldResendDetachMessageOnConnectionResumed(Protocol protocol)
        {
            int detachMessageCount = 0;
            AblyRealtime client = null;
            var channelName = "test-channel".AddRandomSuffix();

            client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.TransportFactory = new TestTransportFactory
                {
                    OnMessageSent = OnMessageSent,
                };
            });

            void OnMessageSent(ProtocolMessage message)
            {
                if (message.Action == ProtocolMessage.MessageAction.Detach)
                {
                    if (detachMessageCount == 0)
                    {
                        detachMessageCount++;
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

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19")]
        [Trait("spec", "RTN19a")]
        public async Task OnConnected_ShouldResendAckWithConnectionMessageSerialIfResumeFailed(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await client.WaitForState(ConnectionState.Connected);
            var initialConnectionId = client.Connection.Id;
            var channel = client.Channels.Get("RTN19a".AddRandomSuffix());

            // var client2 = await GetRealtimeClient(protocol);
            // var channel2 = client2.Channels.Get(channel.Name);
            // await channel2.AttachAsync();
            // var channel2Messages = new List<Message>();
            // channel2.Subscribe(message => channel2Messages.Add(message));

            // Sending dummy messages to increment messageSerial
            await channel.PublishAsync("dummy1", "data1");
            await channel.PublishAsync("dummy2", "data2");

            // This will be unblocked on new connection/transport.
            client.BlockActionFromReceiving(ProtocolMessage.MessageAction.Ack);
            client.BlockActionFromReceiving(ProtocolMessage.MessageAction.Nack);

            var noOfMessagesSent = 10;
            var messageAckAwaiter = new TaskCompletionAwaiter(15000, noOfMessagesSent);
            for (var i = 0; i < noOfMessagesSent; i++)
            {
                channel.Publish("eventName" + i, "data" + i, (success, error) =>
                {
                    if (success)
                    {
                        messageAckAwaiter.Tick();
                    }
                });
            }

            await client.ProcessCommands();
            client.State.WaitingForAck.Count.Should().Be(noOfMessagesSent);
            var initialMessagesIdToSerialMap = client.GetTestTransport()
                .ProtocolMessagesSent.FindAll(message => message.Channel == channel.Name).ToDictionary(m => m.Messages.First().Name, m => m.MsgSerial);

            client.State.Connection.Id = string.Empty;
            client.State.Connection.Key = "xxxxx!xxxxxxx-xxxxxxxx-xxxxxxxx"; // invalid connection key for next resume request
            client.GetTestTransport().Close(false);

            await client.WaitForState(ConnectionState.Connected);
            client.Connection.Id.Should().NotBe(initialConnectionId); // resume not successful

            // Ack received for all messages
            var messagePublishSuccess = await messageAckAwaiter.Task;
            messagePublishSuccess.Should().BeTrue();

            var newMessagesIdToSerialMap = client.GetTestTransport()
                .ProtocolMessagesSent.FindAll(message => message.Channel == channel.Name).ToDictionary(m => m.Messages.First().Name, m => m.MsgSerial);

            // Check for new messageSerial
            foreach (var keyValuePair in newMessagesIdToSerialMap)
            {
                initialMessagesIdToSerialMap[keyValuePair.Key].Should().NotBe(keyValuePair.Value);
            }

            // TODO - Message duplicates can't be detected since messages id's not available
            // channel2Messages.Count.Should().Be(noOfMessagesSent + 2); // first 2 dummy messages

            client.Close();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTN19")]
        [Trait("spec", "RTN19b")]
        public async Task OnConnected_ShouldResendAckWithSameMessageSerialIfResumeSuccessful(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);
            await client.WaitForState(ConnectionState.Connected);
            var initialConnectionId = client.State.Connection.Id;
            var channel = client.Channels.Get("RTN19a".AddRandomSuffix());

            var client2 = await GetRealtimeClient(protocol);
            var channel2 = client2.Channels.Get(channel.Name);
            await channel2.AttachAsync();
            var channel2Messages = new List<Message>();
            channel2.Subscribe(message => channel2Messages.Add(message));

            await channel.PublishAsync("dummy1", "data1");
            await channel.PublishAsync("dummy2", "data2");

            // This will be unblocked on new connection/transport.
            client.BlockActionFromReceiving(ProtocolMessage.MessageAction.Ack);
            client.BlockActionFromReceiving(ProtocolMessage.MessageAction.Nack);

            var noOfMessagesSent = 10;
            var messageAckAwaiter = new TaskCompletionAwaiter(15000, noOfMessagesSent);
            for (var i = 0; i < noOfMessagesSent; i++)
            {
                channel.Publish("eventName" + i, "data" + i, (success, error) =>
                {
                    if (success)
                    {
                        messageAckAwaiter.Tick();
                    }
                });
            }

            await client.ProcessCommands();
            client.State.WaitingForAck.Count.Should().Be(noOfMessagesSent);
            var initialMessagesIdToSerialMap = client.GetTestTransport()
                .ProtocolMessagesSent.FindAll(message => message.Channel == channel.Name).ToDictionary(m => m.Messages.First().Name, m => m.MsgSerial);

            client.GetTestTransport().Close(false); // same connectionKey for next request
            await client.WaitForState(ConnectionState.Connected);
            client.Connection.Id.Should().Be(initialConnectionId); // resume success

            // Ack received for all messages after reconnection
            var messagePublishSuccess = await messageAckAwaiter.Task;
            messagePublishSuccess.Should().BeTrue();

            var newMessagesIdToSerialMap = client.GetTestTransport()
                .ProtocolMessagesSent.FindAll(message => message.Channel == channel.Name).ToDictionary(m => m.Messages.First().Name, m => m.MsgSerial);

            // Check for same messageSerial
            foreach (var keyValuePair in newMessagesIdToSerialMap)
            {
                initialMessagesIdToSerialMap[keyValuePair.Key].Should().Be(keyValuePair.Value);
            }

            // No duplicates found on client2 channel
            channel2Messages.Count.Should().Be(noOfMessagesSent + 2); // first 2 dummy messages

            client.Close();
        }

        public ConnectionSandboxTransportSideEffectsSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }
}
