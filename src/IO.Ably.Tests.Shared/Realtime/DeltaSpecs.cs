using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
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
            RealtimeChannel channel = (RealtimeChannel) c;
            channel.SetChannelState(ChannelState.Attached);
            List<Message> receivedMessages = new List<Message>();
            channel.Subscribe(receivedMessages.Add);

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[] {new Message() {Id = "goodMessage", Data = "test"},},
                }));

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[]
                        {new Message() {Extras = new MessageExtras() {Delta = new DeltaExtras() {From = "1"}}}},
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
            RealtimeChannel channel = (RealtimeChannel) c;
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
                        {new Message() {Extras = new MessageExtras() {Delta = new DeltaExtras() {From = "1"}}}},
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
            RealtimeChannel channel = (RealtimeChannel) c;
            channel.SetChannelState(ChannelState.Attached);

            var successfulProtocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message)
            {
                Channel = channel.Name,
                ChannelSerial = "testSerial",
                Messages = new[] {new Message {Data = "test", Encoding = string.Empty},},
            };
            realtime.ExecuteCommand(ProcessMessageCommand.Create(successfulProtocolMessage));

            var awaiter = new TaskCompletionAwaiter();
            channel.On(ChannelEvent.Attaching, change => { awaiter.Tick(); });

            realtime.ExecuteCommand(ProcessMessageCommand.Create(
                new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channel.Name,
                    Messages = new[]
                        {new Message() {Extras = new MessageExtras() {Delta = new DeltaExtras() {From = "1"}}}},
                }));

            await awaiter.Task;
            await realtime.ProcessCommands();

            var fakeTransport = FakeTransportFactory.LastCreatedTransport;
            var lastMessageSend = fakeTransport.LastMessageSend;
            lastMessageSend.Should().NotBeNull();
            lastMessageSend.Action.Should().Be(ProtocolMessage.MessageAction.Attach);
            lastMessageSend.ChannelSerial.Should().Be("testSerial");
        }

        public DeltaSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
