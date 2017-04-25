using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("Presence Sandbox")]
    [Trait("requires", "sandbox")]
    public class PresenceSandboxSpecs : SandboxSpecs
    {
        public class GeneralPresenceSandBoxSpecs : PresenceSandboxSpecs
        {
            public GeneralPresenceSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
            {
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP1")]
            public async Task WhenAttachingToAChannelWithNoMembers_PresenceShouldBeConsideredInSync(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol);
                var channel = client.Channels.Get(GetTestChannelName());

                await channel.AttachAsync();

                channel.Presence.SyncComplete.Should().BeTrue();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP1")]
            public async Task WhenAttachingToAChannelWithMembers_PresenceShouldBeInProgress(Protocol protocol)
            {
                Logger.LogLevel = LogLevel.Debug;
                var testChannel = GetTestChannelName();
                var client = await GetRealtimeClient(protocol);
                var client2 = await GetRealtimeClient(protocol);
                var channel = client.Channels.Get(testChannel);
                List<Task> tasks = new List<Task>();
                for (int count = 1; count < 10; count++)
                {
                    tasks.Add(channel.Presence.EnterClientAsync($"client-{count}", null));
                }

                Task.WaitAll(tasks.ToArray());

                var channel2 = client2.Channels.Get(testChannel) as RealtimeChannel;

                bool inSync = channel2.Presence.Map.IsSyncInProgress;
                bool syncComplete = channel2.Presence.SyncComplete;

                channel2.InternalStateChanged += (_, args) =>
                {
                    if (args.Current == ChannelState.Attached)
                    {
                        Logger.Debug("Test: Setting inSync to - " + channel2.Presence.Map.IsSyncInProgress);
                        inSync = channel2.Presence.Map.IsSyncInProgress;
                        syncComplete = channel2.Presence.SyncComplete;
                    }
                };

                await channel2.AttachAsync();

                inSync.Should().BeTrue();
                syncComplete.Should().BeFalse();
            }

            [Theory]
            [ProtocolData]
            public async Task CanSend_EnterWithStringArray(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "test");

                var channel = client.Channels.Get("test" + protocol);

                await channel.Presence.EnterAsync(new[] { "test", "best" });

                var presence = await channel.Presence.GetAsync();
                await Task.Delay(2000);
                presence.Should().HaveCount(1);
            }

            [Theory]
            [ProtocolData]
            public async Task Presence_HasCorrectTimeStamp(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "presence-timestamp-test");

                var channel = client.Channels.Get("test".AddRandomSuffix());
                DateTimeOffset? time = null;
                channel.Presence.Subscribe(message =>
                {
                    Output.WriteLine($"{message.ConnectionId}:{message.Timestamp}");
                    time = message.Timestamp;
                    _resetEvent.Set();
                });

                await channel.Presence.EnterAsync(new[] { "test", "best" });

                _resetEvent.WaitOne(2000);
                time.Should().HaveValue();
            }
        }

        public class With250PresentMembersOnAChannel : PresenceSandboxSpecs
        {
            private const int ExpectedEnterCount = 250;
            private string _channelName;

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP4")]
            public async Task WhenAClientAttachedToPresenceChannel_ShouldEmitPresentForEachMember(Protocol protocol)
            {
                await SetupMembers(protocol);
                var testClient = await GetRealtimeClient(protocol);
                var channel = testClient.Channels.Get(_channelName);

                List<PresenceMessage> presenceMessages = new List<PresenceMessage>();
                channel.Presence.Subscribe(x => presenceMessages.Add(x));

                //Wait for 30s max
                int count = 0;
                while (count < 30)
                {
                    count++;

                    if (presenceMessages.Count == ExpectedEnterCount)
                        return;

                    await Task.Delay(1000);
                }

                throw new Exception("Failed to receive messages for all memebers");
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP2")]
            public async Task WhenAMemberLeavesBeforeSYNCOperationIsComplete_ShouldEmitLeaveMessageForMember(
                Protocol protocol)
            {
                Logger.LogLevel = LogLevel.Debug;

                await SetupMembers(protocol);
                var client = await GetRealtimeClient(protocol);
                var channel = client.Channels.Get(_channelName);

                ConcurrentBag<PresenceMessage> presenceMessages = new ConcurrentBag<PresenceMessage>();
                string leaveClientId = "";
                channel.Presence.Subscribe(PresenceAction.Present, x =>
                {
                    Logger.Debug($"[{client.Connection.Id}] Adding message #" + (presenceMessages.Count + 1));
                    presenceMessages.Add(x);
                });
                channel.Presence.Subscribe(PresenceAction.Leave, x => leaveClientId = x.ClientId);

                await new ConnectionAwaiter(client.Connection, ConnectionState.Connected).Wait();

                SendLeaveMessageAfterFirstSyncMessageReceived(client, GetClientId(0));

                //Wait for 30s max
                await WaitFor30sOrUntilTrue(() =>
                {
                    var count = presenceMessages.Count();
                    Logger.Debug("Presence message count: " + count);
                    return presenceMessages.Count() == ExpectedEnterCount;
                });

                presenceMessages.Count.Should().Be(ExpectedEnterCount);
                channel.Presence.SyncComplete.Should().BeTrue();
                leaveClientId.Should().Be(GetClientId(0));
            }

            private void SendLeaveMessageAfterFirstSyncMessageReceived(AblyRealtime client, string clientId)
            {
                var transport = client.GetTestTransport();

                int syncMessageCount = 0;
                transport.AfterDataReceived = message =>
                {
                    if (message.Action == ProtocolMessage.MessageAction.Sync)
                    {
                        syncMessageCount++;
                        if (syncMessageCount == 1)
                        {
                            var leaveMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, _channelName)
                            {
                                Presence = new[]
                                {
                                    new PresenceMessage(PresenceAction.Leave, clientId)
                                    {
                                        ConnectionId = $"{client.Connection.Id}",
                                        Id = $"{client.Connection.Id}-#{clientId}:0",
                                        Timestamp = Config.Now(),
                                    }
                                }
                            };
                            transport.FakeReceivedMessage(leaveMessage);
                        }
                    }
                };
            }

            private async Task SetupMembers(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol);
                var channel = client.Channels.Get(_channelName);
                for (int i = 0; i < ExpectedEnterCount; i++)
                {
                    var clientId = GetClientId(i);
                    await channel.Presence.EnterClientAsync(clientId, null);
                }
            }

            private string GetClientId(int count)
            {
                return "client:#" + count;
            }

            public With250PresentMembersOnAChannel(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
            {
                _channelName = GetTestChannelName();
            }

        }



        public PresenceSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }

        protected string GetTestChannelName()
        {
            return "presence-" + Guid.NewGuid().ToString().Split('-').First();
        }
    }
}