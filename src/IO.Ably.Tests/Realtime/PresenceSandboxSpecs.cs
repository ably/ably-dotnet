using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
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


            //TODO: Add tests to makes sure Presense messages id, timestamp and connectionId are set
            

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP1")]
            public async Task WhenAttachingToAChannelWithNoMembers_PresenceShouldBeConsideredInSync(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol);
                var channel = client.Channels.Get(GetTestChannelName());

                await channel.AttachAsync();
                await channel.WaitForState(ChannelState.Attached);

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

                await Task.WhenAll(tasks.ToArray());

                var channel2 = client2.Channels.Get(testChannel) as RealtimeChannel;
                int inSync = 0;
                int syncComplete = 0;

                channel2.InternalStateChanged += (_, args) =>
                {
                    if (args.Current == ChannelState.Attached)
                    {
                        Logger.Debug("Test: Setting inSync to - " + channel2.Presence.Map.IsSyncInProgress);
                        Interlocked.Add(ref inSync, channel2.Presence.Map.IsSyncInProgress ? 1: 0);
                        Interlocked.Add(ref syncComplete, channel2.Presence.SyncComplete ? 1: 0);
                    }
                };

                await channel2.AttachAsync();
                await Task.Delay(1000);
                inSync.Should().Be(1);
                syncComplete.Should().Be(0);
            }

            /*
	        * Test presence message map behaviour (RTP2 features)
	        * Tests RTP2a, RTP2b1, RTP2b2, RTP2c, RTP2d, RTP2g, RTP18c, RTP6a features
	        */
            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP2")]
            [Trait("spec", "RTP2a")]
            [Trait("spec", "RTP2b1")]
            [Trait("spec", "RTP2b2")]
            [Trait("spec", "RTP2c")]
            [Trait("spec", "RTP2d")]
            [Trait("spec", "RTP2g")]
            [Trait("spec", "RTP6a")]
            [Trait("spec", "RTP18c")]
            public async Task PresenceMapBehaviour_ShouldConformToSpec(Protocol protocol)
            {
                Logger.LogLevel = LogLevel.Debug;

                var channelName = "presence_map_tests_newness".AddRandomSuffix();

                var client = await GetRealtimeClient(protocol);
                await client.WaitForState(ConnectionState.Connected);
                client.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channel = client.Channels.Get(channelName);
                channel.Attach();
                await channel.WaitForState(ChannelState.Attached);
                channel.State.ShouldBeEquivalentTo(ChannelState.Attached);

                const string wontPass = "Won't pass newness test";
                
                List<PresenceMessage> presenceMessages = new List<PresenceMessage>();
                channel.Presence.Subscribe(x =>
                {
                    x.Data.Should().NotBe(wontPass, "message did not pass the newness test");
                    presenceMessages.Add(x);
                });

                /* Test message newness criteria as described in RTP2b */
                PresenceMessage[] testData = new PresenceMessage[] {
                    new PresenceMessage
                    {
                        Action = PresenceAction.Enter,
                        ClientId = "1",
                        ConnectionId = "1",
                        Id = "1:0",
                        Data = string.Empty
                    },
                    new PresenceMessage
                    {
                        Action = PresenceAction.Enter,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:1:0",
                        Timestamp = new DateTimeOffset(2000,1,1,1,1,1, new TimeSpan()),
                        Data = string.Empty
                    },
                    /* Should be newer than previous one */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:2:1",
                        Timestamp = new DateTimeOffset(2000,1,1,1,1,2, new TimeSpan()),
                        Data = string.Empty
                    }, 
                    /* Shouldn't pass newness test because of message serial, timestamp doesn't matter in this case */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:1:1",
                        Timestamp = new DateTimeOffset(2000,1,1,1,1,3, new TimeSpan()),
                        Data = wontPass
                    }, 
                    /* Shouldn't pass because of message index */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:2:0",
                        Data = wontPass
                    },
                    /* Should pass because id is not in form connId:clientId:index and timestamp is greater */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "wrong_id",
                        Timestamp = new DateTimeOffset(2000,1,1,1,1,10, new TimeSpan()),
                        Data = string.Empty
                    },
                    /* Shouldn't pass because of timestamp */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:3:1",
                        Timestamp = new DateTimeOffset(2000,1,1,1,1,5, new TimeSpan()),
                        Data = wontPass
                    }
                };

                foreach (var presenceMessage in testData)
                {
                    var protocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
                    {
                        Channel = channelName,
                        Presence = new PresenceMessage[] {presenceMessage}
                    };
                    await client.Connection.ConnectionManager.OnTransportMessageReceived(protocolMessage);
                }

                int n = 0;
                foreach (var testMsg in testData)
                {
                    if (testMsg.Data.ToString() == wontPass) continue;
                    PresenceMessage factualMsg = n < presenceMessages.Count ? presenceMessages[n++] : null;
                    factualMsg.Should().NotBe(null);
                    factualMsg.Id.ShouldBeEquivalentTo(testMsg.Id);
                    factualMsg.Action.ShouldBeEquivalentTo(testMsg.Action, "message was not emitted on the presence object with original action");
                    var presentMessage = await channel.Presence.GetAsync(new GetOptions
                    {
                        ClientId = testMsg.ClientId, WaitForSync = false
                    });
                    presentMessage.FirstOrDefault().Should().NotBe(null);
                    presentMessage.FirstOrDefault()?.Action.ShouldBeEquivalentTo(PresenceAction.Present, "message was not added to the presence map and stored with PRESENT action");
                }

                presenceMessages.Count.ShouldBeEquivalentTo(n, "the number of messages received didn't match the number of test messages sent.");
                
                /* Repeat the process now as a part of SYNC and verify everything is exactly the same */
                var channel2Name = "presence_map_tests_sync_newness".AddRandomSuffix();

                var client2 = await GetRealtimeClient(protocol);
                await client2.WaitForState(ConnectionState.Connected);
                client2.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channel2 = client2.Channels.Get(channel2Name);
                channel2.Attach();
                await channel2.WaitForState(ChannelState.Attached);
                channel2.State.ShouldBeEquivalentTo(ChannelState.Attached);

                /* Send all the presence data in one SYNC message without channelSerial (RTP18c) */
                ProtocolMessage syncMessage = new ProtocolMessage() {
                        Channel = channel2Name,
                        Action = ProtocolMessage.MessageAction.Sync,
                        Presence = testData
                };

                var counter = new TaskCountAwaiter(presenceMessages.Count, 5000);
                List<PresenceMessage> syncPresenceMessages = new List<PresenceMessage>();
                channel2.Presence.Subscribe(x =>
                {
                    x.Data.Should().NotBe(wontPass, "message did not pass the newness test");
                    syncPresenceMessages.Add(x);
                    counter.Tick();
                });

                await client2.Connection.ConnectionManager.OnTransportMessageReceived(syncMessage);
                await counter.Task;
                
                syncPresenceMessages.Count.ShouldBeEquivalentTo(presenceMessages.Count);

                for (int i = 0; i < syncPresenceMessages.Count; i++)
                {
                    syncPresenceMessages[i].Id.ShouldBeEquivalentTo(presenceMessages[i].Id, "result should be the same in case of SYNC");
                    syncPresenceMessages[i].Action.ShouldBeEquivalentTo(presenceMessages[i].Action, "result should be the same in case of SYNC");
                }
            }
            
            [Theory]
            [ProtocolData]
            public async Task CanSend_EnterWithStringArray(Protocol protocol)
            {
                var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "test");

                var channel = client.Channels.Get("test" + protocol);

                await channel.Presence.EnterAsync(new[] { "test", "best" });

                await Task.Delay(2000);
                var presence = await channel.Presence.GetAsync();
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

            /*
             * Ensure a test exists that enters 250 members using Presence#enterClient on a single connection,
             * and checks for PRESENT events to be emitted on another connection for each member,
             * and once sync is complete, all 250 members should be present in a Presence#get request
             */

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP4")]
            public async Task WhenAClientAttachedToPresenceChannel_ShouldEmitPresentForEachMember(Protocol protocol)
            {
                var channelName = "presence".AddRandomSuffix();

                var clientA = await GetRealtimeClient(protocol);
                await clientA.WaitForState(ConnectionState.Connected);
                clientA.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channelA = clientA.Channels.Get(channelName);
                channelA.Attach();
                await channelA.WaitForState(ChannelState.Attached);
                channelA.State.ShouldBeEquivalentTo(ChannelState.Attached);

                //  enters 250 members on a single connection A
                for (int i = 0; i < ExpectedEnterCount; i++)
                {
                    var clientId = GetClientId(i);
                    await channelA.Presence.EnterClientAsync(clientId, null);
                }

                var clientB = await GetRealtimeClient(protocol);
                await clientB.WaitForState(ConnectionState.Connected);
                clientB.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channelB = clientB.Channels.Get(channelName);
                channelB.Attach();
                await channelB.WaitForState(ChannelState.Attached);
                channelB.State.ShouldBeEquivalentTo(ChannelState.Attached);

                // checks for PRESENT events to be emitted on another connection for each member
                List<PresenceMessage> presenceMessages = new List<PresenceMessage>();
                var awaiter = new TaskCompletionAwaiter(timeoutMs: 200000);
                channelB.Presence.Subscribe(x =>
                {
                    presenceMessages.Add(x);
                    if (presenceMessages.Count == ExpectedEnterCount)
                    {
                        awaiter.SetCompleted();
                    }
                });
                var received250MessagesBeforeTimeout = await awaiter.Task;
                received250MessagesBeforeTimeout.ShouldBeEquivalentTo(true);

                // all 250 members should be present in a Presence#get request
                var messages = await channelB.Presence.GetAsync(new GetOptions{WaitForSync = true});
                var messageList = messages as IList<PresenceMessage> ?? messages.ToList();
                messageList.Count().ShouldBeEquivalentTo(ExpectedEnterCount);
                foreach (var m in messageList)
                {
                    presenceMessages.Select(x => x.ClientId == m.ClientId).Any().Should().BeTrue();
                }

                clientA.Close();
                clientB.Close();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP2")]
            public async Task WhenAMemberLeavesBeforeSYNCOperationIsComplete_ShouldEmitLeaveMessageForMember(
                Protocol protocol)
            {
                Logger.LogLevel = LogLevel.Debug;
                var channelName = "presence".AddRandomSuffix();

                var clientA = await GetRealtimeClient(protocol);
                await clientA.WaitForState(ConnectionState.Connected);
                clientA.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channelA = clientA.Channels.Get(channelName);
                channelA.Attach();
                await channelA.WaitForState(ChannelState.Attached);
                channelA.State.ShouldBeEquivalentTo(ChannelState.Attached);

                //  enters 250 members on a single connection A
                for (int i = 0; i < ExpectedEnterCount; i++)
                {
                    var clientId = GetClientId(i);
                    await channelA.Presence.EnterClientAsync(clientId, null);
                }

                var clientB = await GetRealtimeClient(protocol);
                await clientB.WaitForState(ConnectionState.Connected);
                clientB.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channelB = clientB.Channels.Get(channelName);
                channelB.Attach();
                await channelB.WaitForState(ChannelState.Attached);
                channelB.State.ShouldBeEquivalentTo(ChannelState.Attached);

                ConcurrentBag<PresenceMessage> presenceMessages = new ConcurrentBag<PresenceMessage>();
                string leaveClientId = "";
                var awaiter = new TaskCompletionAwaiter(timeoutMs: 200000);
                channelB.Presence.Subscribe(PresenceAction.Present, x =>
                {
                    Logger.Debug($"[{clientB.Connection.Id}] Adding message #" + (presenceMessages.Count + 1));
                    presenceMessages.Add(x);
                    if (presenceMessages.Count == ExpectedEnterCount)
                    {
                        awaiter.SetCompleted();
                    }
                });
                channelB.Presence.Subscribe(PresenceAction.Leave, x => leaveClientId = x.ClientId);

                await clientB.WaitForState(ConnectionState.Connected);

                SendLeaveMessageAfterFirstSyncMessageReceived(clientB, GetClientId(0), channelName);

                //Wait for 30s max
                await WaitFor30sOrUntilTrue(() =>
                {
                    var count = presenceMessages.Count();
                    Logger.Debug("Presence message count: " + count);
                    return presenceMessages.Count() == ExpectedEnterCount;
                });

                presenceMessages.Count.Should().Be(ExpectedEnterCount);
                channelB.Presence.SyncComplete.Should().BeTrue();
                leaveClientId.Should().Be(GetClientId(0));
            }

            private void SendLeaveMessageAfterFirstSyncMessageReceived(AblyRealtime client, string clientId, string channelName)
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
                            var leaveMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, channelName)
                            {
                                Presence = new[]
                                {
                                    new PresenceMessage(PresenceAction.Leave, clientId)
                                    {
                                        ConnectionId = $"{client.Connection.Id}",
                                        Id = $"{client.Connection.Id}-#{clientId}:0",
                                        Timestamp = TestHelpers.Now(),
                                    }
                                }
                            };
                            transport.FakeReceivedMessage(leaveMessage);
                        }
                    }
                };
            }

            private async Task SetupMembers(Protocol protocol, string channelName)
            {
                var client = await GetRealtimeClient(protocol);
                var channel = client.Channels.Get(channelName);
                for (int i = 0; i < ExpectedEnterCount; i++)
                {
                    var clientId = GetClientId(i);
                    await channel.Presence.EnterClientAsync(clientId, null);
                }
            }

            private string GetClientId(int count)
            {
                return "client:#" + count.ToString().PadLeft(3, '0');
            }

            public With250PresentMembersOnAChannel(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
            {
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
 