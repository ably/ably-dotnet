using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.AcceptanceTests;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Tests.DotNetCore20.Infrastructure;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Types;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Realtime
{
    [Collection("SandBox Connection")]
    [Trait("type", "integration")]
    public class DeltaSandboxSpecs : SandboxSpecs
    {
        public class TestData
        {
            public string Foo { get; }

            public int Count { get; set; }

            public string Status { get; set; }

            public TestData(string foo, int count, string status)
            {
                Foo = foo;
                Count = count;
                Status = status;
            }

            public override string ToString()
            {
                return "foo = " + Foo + "; count = " + Count + "; status = " + Status;
            }

            protected bool Equals(TestData other)
            {
                return Foo == other.Foo && Count == other.Count && Status == other.Status;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((TestData)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Foo != null ? Foo.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ Count;
                    hashCode = (hashCode * 397) ^ (Status != null ? Status.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        [Theory]
        [ProtocolData]
        public async Task DeltaSupport_ShouldWork(Protocol protocol)
        {
            string testName = "delta-channel".AddRandomSuffix();
            Logger.LogLevel = LogLevel.Debug;
            Logger.LoggerSink = new OutputLoggerSink(Output);
            var realtime = await GetRealtimeClient(protocol);
            var channel = realtime.Channels.Get("[?delta=vcdiff]" + testName);

            var received = new List<Message>();
            channel.Subscribe(message => { received.Add(message); });
            channel.Error += (sender, args) =>
                throw new Exception(args.Reason.Message);
            /* subscribe */
            await channel.AttachAsync();

            var testData = new[]
            {
                new TestData("bar", 1, "active"),
                new TestData("bar", 2, "active"),
                new TestData("bar", 3, "inactive"),
            };

            foreach (var data in testData)
            {
                await channel.PublishAsync(data.Count.ToString(), data);
            }

            await new ConditionalAwaiter(() => received.Count == 3);

            for (var i = 0; i < received.Count; i++)
            {
                var message = received[i];
                var data = ((JToken)message.Data).ToObject<TestData>();
                var original = testData[i];
                Assert.Equal(data, original);
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSL6c")]
        [Trait("spec", "RSL6c1")]
        public async Task WhenDeltaDecodeFail_ShouldSetStateToAttachingLogTheErrorAndDiscardTheMessage(Protocol protocol)
        {
            string channelName = "[?delta=vcdiff]delta-channel".AddRandomSuffix();
            var testSink = new TestLoggerSink();
            using (((DefaultLogger.InternalLogger)Logger).SetTempDestination(testSink))
            {
                var realtime = await GetRealtimeClient(protocol);
                var channel = realtime.Channels.Get(channelName);

                var received = new List<Message>();
                channel.Subscribe(message => { received.Add(message); });

                await channel.AttachAsync();
                // We wait for the first message to be acknowledged. Any consequent messages will be deltas
                await channel.PublishAsync("firstMessage", new TestData("bar", 1, "active"));
                channel.Publish("second", "second message"); // We don't want to wait for the acknowledgement
                realtime.ExecuteCommand(ProcessMessageCommand.Create(new ProtocolMessage(ProtocolMessage.MessageAction.Message)
                {
                    Channel = channelName,
                    Messages = new[]
                    {
                        new Message()
                        {
                            Id = "badMessage", Encoding = "vcdiff", Data = new byte[0]
                        },
                    },
                }));

                await channel.WaitForState(ChannelState.Attaching);

                await channel.WaitForState(ChannelState.Attached);

                await new ConditionalAwaiter(() => received.Count() == 2);

                received.Should().HaveCount(2);
                received[1].Data.Should().Be("second message");

                // RSL6c2 - we make sure the message is not emitted to subscribers
                received.Should().NotContain(x => x.Id == "badMessage");

                bool hasVcdiffErrorMessage = testSink.Messages.Any(x => x.StartsWith(LogLevel.Error.ToString())
                                                                        && x.Contains(ErrorCodes.VCDiffDecodeError
                                                                            .ToString()));

                hasVcdiffErrorMessage.Should().BeTrue();
            }
        }

        public DeltaSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }
}
