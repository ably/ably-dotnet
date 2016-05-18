using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class ChannelSandboxSpecs : SandboxSpecs
    {
        [Theory]
        [ProtocolData]
        public async Task TestGetChannel_ReturnsValidChannel(Protocol protocol)
        {
            // Arrange
            var client = await GetRealtimeClient(protocol);

            // Act
            IRealtimeChannel target = client.Channels.Get("test");

            // Assert
            target.Name.ShouldBeEquivalentTo("test");
            target.State.ShouldBeEquivalentTo(ChannelState.Initialized);
        }

        [Theory]
        [ProtocolData]
        public async Task TestAttachChannel_AttachesSuccessfuly(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            // Arrange
            var client = await GetRealtimeClient(protocol);
            Semaphore signal = new Semaphore(0, 2);
            var args = new List<ChannelStateChangedEventArgs>();
            IRealtimeChannel target = client.Channels.Get("test");
            target.ChannelStateChanged += (s, e) =>
            {
                args.Add(e);
                signal.Release();
            };

            // Act
            target.Attach();

            // Assert
            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(1);
            args[0].NewState.ShouldBeEquivalentTo(ChannelState.Attaching);
            args[0].Reason.ShouldBeEquivalentTo(null);
            target.State.ShouldBeEquivalentTo(ChannelState.Attaching);

            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(2);
            args[1].NewState.ShouldBeEquivalentTo(ChannelState.Attached);
            args[1].Reason.ShouldBeEquivalentTo(null);
            target.State.ShouldBeEquivalentTo(ChannelState.Attached);
        }

        [Theory]
        [ProtocolData]
        public async Task TestAttachChannel_SendingMessage_EchoesItBack(Protocol protocol)
        {
            // Arrange
            var client = await GetRealtimeClient(protocol);
            IRealtimeChannel target = client.Channels.Get("test");
            target.Attach();
            var messagesReceived = new List<Message>();
            target.Subscribe(messages =>
            {
                messagesReceived.AddRange(messages);
            });

            // Act
            target.Publish("test", "test data");

            await Task.Delay(2000);

            // Assert
            messagesReceived.Count.ShouldBeEquivalentTo(1);
            messagesReceived[0].name.ShouldBeEquivalentTo("test");
            messagesReceived[0].data.ShouldBeEquivalentTo("test data");
        }

        [Theory]
        [ProtocolData]
        public async Task TestAttachChannel_Sending3Messages_EchoesItBack(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            // Arrange
            var client = await GetRealtimeClient(protocol);
            AutoResetEvent signal = new AutoResetEvent(false);
            IRealtimeChannel target = client.Channels.Get("test");
            target.Attach();
            List<Message> messagesReceived = new List<Message>();
            target.Subscribe(messages =>
            {
                messagesReceived.AddRange(messages);
            });

            // Act
            target.Publish("test1", "test 12");
            target.Publish("test2", "test 123");
            target.Publish("test3", "test 321");

            await Task.Delay(2000);
            // Assert
            messagesReceived.Count.ShouldBeEquivalentTo(3);
            messagesReceived[0].name.ShouldBeEquivalentTo("test1");
            messagesReceived[0].data.ShouldBeEquivalentTo("test 12");
            messagesReceived[1].name.ShouldBeEquivalentTo("test2");
            messagesReceived[1].data.ShouldBeEquivalentTo("test 123");
            messagesReceived[2].name.ShouldBeEquivalentTo("test3");
            messagesReceived[2].data.ShouldBeEquivalentTo("test 321");
        }

        [Theory]
        [ProtocolData]
        public async Task TestAttachChannel_SendingMessage_Doesnt_EchoesItBack(Protocol protocol)
        {
            // Arrange
            var client = await GetRealtimeClient(protocol, o => o.EchoMessages = false);
            AutoResetEvent signal = new AutoResetEvent(false);
            var target = client.Channels.Get("test");

            target.Attach();

            List<Message> messagesReceived = new List<Message>();
            target.Subscribe(messages =>
            {
                messagesReceived.AddRange(messages);
                signal.Set();
            });

            // Act
            target.Publish("test", "test data");
            signal.WaitOne(10000);

            // Assert
            messagesReceived.Count.ShouldBeEquivalentTo(0);
        }

        public ChannelSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }
}
