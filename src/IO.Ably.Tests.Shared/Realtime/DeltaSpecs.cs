using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Tests.DotNetCore20.Infrastructure;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Realtime
{
    public class DeltaSpecs : AblyRealtimeSpecs
    {
        [Fact]
        [Trait("spec", "RSL6c2")]
        public async Task WhenMessageRecevied_WithDeltaError_ShouldNotPassMessageToChannelSubscriber()
        {
            var (realtime, c) = await GetClientAndChannel();
            RealtimeChannel channel = (RealtimeChannel)c;
            channel.SetChannelState(ChannelState.Attached);
            List<Message> receivedMessages = new List<Message>();
            channel.Subscribe(receivedMessages.Add);

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[] { new Message() { Id = "goodMessage", Data = "test" }, },
                }));

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[]
                    {
                        new Message() { Extras = new MessageExtras() { Delta = new DeltaExtras() { From = "1" } } },
                    },
                }));

            await Task.Delay(2000); // wait for 2 seconds

            // We want to ensure just the good message was received
            receivedMessages.Should().HaveCount(1);
            receivedMessages.Should().Contain(x => x.Id == "goodMessage");
        }

        [Fact]
        [Trait("spec", "RSL6c3")]
        [Trait("spec", "RSL6e")]
        public async Task
            WhenMessageReceived_WithNotMatchingDeltaFromProperty_ShouldStartDecodeRecoveryAndMoveToAttachingWithError()
        {
            var (realtime, c) = await GetClientAndChannel();
            RealtimeChannel channel = (RealtimeChannel)c;
            channel.SetChannelState(ChannelState.Attached);
            var awaiter = new TaskCompletionAwaiter();
            ChannelStateChange stateChange = null;
            channel.On(ChannelEvent.Attaching, change =>
            {
                stateChange = change;
                channel.DecodeRecovery.Should().BeTrue();
                awaiter.Done();
            });

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[]
                    {
                        new Message { Extras = new MessageExtras() { Delta = new DeltaExtras() { From = "1" } } },
                    },
                }));

            await awaiter.Task;

            stateChange.Current.Should().Be(ChannelState.Attaching);
            stateChange.Error.Code.Should().Be(ErrorCodes.VCDiffDecodeError);
        }

        [Fact]
        [Trait("spec", "RSL6c3")]
        public async Task
            WhenMessageReceivedAndFailsVcdiffDecoding_ShouldSendAttachProtocolMessageWithLastSuccessfulChannelSerial()
        {
            var (realtime, c) = await GetClientAndChannel();
            RealtimeChannel channel = (RealtimeChannel)c;
            channel.SetChannelState(ChannelState.Attached);

            var successfulProtocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message)
            {
                Channel = channel.Name,
                ChannelSerial = "testSerial",
                Messages = new[] { new Message { Data = "test", Encoding = string.Empty }, },
            };
            realtime.ExecuteCommand(ProcessMessageCommand.Create(successfulProtocolMessage));

            var awaiter = new TaskCompletionAwaiter();
            channel.On(ChannelEvent.Attaching, change => { awaiter.Tick(); });

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[]
                    {
                        new Message() { Extras = new MessageExtras() { Delta = new DeltaExtras() { From = "1" } } },
                    },
                }));

            await awaiter.Task;
            await realtime.ProcessCommands();

            var fakeTransport = FakeTransportFactory.LastCreatedTransport;
            var lastMessageSend = fakeTransport.LastMessageSend;
            lastMessageSend.Should().NotBeNull();
            lastMessageSend.Action.Should().Be(ProtocolMessage.MessageAction.Attach);
            lastMessageSend.ChannelSerial.Should().Be("testSerial");
        }

        [Fact]
        [Trait("spec", "RSL6f")]
        public async Task
            WhenMultipleMessagesWithAMixOfNormalAndDeltaMessages_ShouldDecodeCorrectly()
        {
            var (realtime, c) = await GetClientAndChannel();
            RealtimeChannel channel = (RealtimeChannel)c;
            channel.SetChannelState(ChannelState.Attached);

            List<Message> messages = new List<Message>();
            channel.Subscribe(messages.Add);
            var protocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message)
            {
                Channel = channel.Name,
                Messages = new[]
                {
                    CreateMessage("delta.1", false),
                    CreateMessage("delta.1.vcdiff", true),
                    CreateMessage("delta.2.vcdiff", true),
                    CreateMessage("delta.3.vcdiff", true),
                },
            };

            await realtime.ProcessMessage(protocolMessage);

            var awaiter = new ConditionalAwaiter(() => messages.Count == 4);
            await awaiter;

            messages[0].Data.Should().BeOfType<string>();
            IsCorrectFile(((string)messages[0].Data).GetBytes(), "delta.1");
            messages[1].Data.Should().BeOfType<byte[]>();
            IsCorrectFile((byte[])messages[1].Data, "delta.2");
            messages[2].Data.Should().BeOfType<byte[]>();
            IsCorrectFile((byte[])messages[2].Data, "delta.3");
            messages[3].Data.Should().BeOfType<byte[]>();
            IsCorrectFile((byte[])messages[3].Data, "delta.4");

            void IsCorrectFile(byte[] actual, string expectedFile)
            {
                actual.SequenceEqual(ResourceHelper.GetBinaryResource(expectedFile)).Should().BeTrue("Bytes are not the same as " + expectedFile);
            }

            Message CreateMessage(string filename, bool isDelta)
            {
                if (isDelta)
                {
                    return new Message()
                    {
                        Data = ResourceHelper.GetBinaryResource(filename).ToBase64(),
                        Encoding = "vcdiff/base64",
                    };
                }

                return new Message()
                {
                    Data = ResourceHelper.GetResource(filename),
                    Encoding = string.Empty,
                };
            }
        }

        public DeltaSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
