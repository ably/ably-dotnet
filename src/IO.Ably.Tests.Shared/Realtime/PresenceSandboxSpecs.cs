using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
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
            public GeneralPresenceSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
                : base(fixture, output)
            {
            }

            // TODO: Add tests to makes sure Presense messages id, timestamp and connectionId are set
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
                        Interlocked.Add(ref inSync, channel2.Presence.Map.IsSyncInProgress ? 1 : 0);
                        Interlocked.Add(ref syncComplete, channel2.Presence.InternalSyncComplete ? 1 : 0);
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
                PresenceMessage[] testData = new PresenceMessage[]
                {
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
                        Timestamp = new DateTimeOffset(2000, 1, 1, 1, 1, 1, default(TimeSpan)),
                        Data = string.Empty
                    },
                    /* Should be newer than previous one */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:2:1",
                        Timestamp = new DateTimeOffset(2000, 1, 1, 1, 1, 2, default(TimeSpan)),
                        Data = string.Empty
                    },
                    /* Shouldn't pass newness test because of message serial, timestamp doesn't matter in this case */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:1:1",
                        Timestamp = new DateTimeOffset(2000, 1, 1, 1, 1, 3, default(TimeSpan)),
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
                        Timestamp = new DateTimeOffset(2000, 1, 1, 1, 1, 10, default(TimeSpan)),
                        Data = string.Empty
                    },
                    /* Shouldn't pass because of timestamp */
                    new PresenceMessage
                    {
                        Action = PresenceAction.Update,
                        ClientId = "2",
                        ConnectionId = "2",
                        Id = "2:3:1",
                        Timestamp = new DateTimeOffset(2000, 1, 1, 1, 1, 5, default(TimeSpan)),
                        Data = wontPass
                    }
                };

                foreach (var presenceMessage in testData)
                {
                    var protocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
                    {
                        Channel = channelName,
                        Presence = new PresenceMessage[] { presenceMessage }
                    };
                    await client.Connection.ConnectionManager.OnTransportMessageReceived(protocolMessage);
                }

                int n = 0;
                foreach (var testMsg in testData)
                {
                    if (testMsg.Data.ToString() == wontPass)
                    {
                        continue;
                    }

                    PresenceMessage factualMsg = n < presenceMessages.Count ? presenceMessages[n++] : null;
                    factualMsg.Should().NotBe(null);
                    factualMsg.Id.ShouldBeEquivalentTo(testMsg.Id);
                    factualMsg.Action.ShouldBeEquivalentTo(testMsg.Action, "message was not emitted on the presence object with original action");
                    var presentMessage = await channel.Presence.GetAsync(new Presence.GetParams
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
                ProtocolMessage syncMessage = new ProtocolMessage()
                {
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

            /*
             * If a SYNC is in progress, then when a presence message with an action of LEAVE arrives,
             * it should be stored in the presence map with the action set to ABSENT.
             * When the SYNC completes, any ABSENT members should be deleted from the presence map.
             * (This is because in a SYNC, we might receive a LEAVE before the corresponding ENTER)
             */
            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP2f")]
            [Trait("spec", "RTP18a")]
            [Trait("spec", "RTP18b")]
            public async Task PresenceMap_WhenSyncingLeaveStoredAsAbsentAndDeleted(Protocol protocol)
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

                PresenceMessage[] TestPresence1()
                {
                    return new PresenceMessage[]
                    {
                        new PresenceMessage
                        {
                            Action = PresenceAction.Enter,
                            ClientId = "1",
                            ConnectionId = "1",
                            Id = "1:0",
                            Data = string.Empty
                        },
                    };
                }

                PresenceMessage[] TestPresence2()
                {
                    return new PresenceMessage[]
                    {
                        new PresenceMessage
                        {
                            Action = PresenceAction.Enter,
                            ClientId = "2",
                            ConnectionId = "2",
                            Id = "2:1:0",
                            Data = string.Empty
                        },
                        new PresenceMessage
                        {
                            Action = PresenceAction.Enter,
                            ClientId = "3",
                            ConnectionId = "3",
                            Id = "3:1:0",
                            Data = string.Empty
                        },
                    };
                }

                PresenceMessage[] TestPresence3()
                {
                    return new PresenceMessage[]
                    {
                        new PresenceMessage
                        {
                            Action = PresenceAction.Leave,
                            ClientId = "3",
                            ConnectionId = "3",
                            Id = "3:0:0",
                            Data = string.Empty
                        },
                        new PresenceMessage
                        {
                            Action = PresenceAction.Enter,
                            ClientId = "4",
                            ConnectionId = "4",
                            Id = "4:1:1",
                            Data = string.Empty
                        },
                        new PresenceMessage
                        {
                            Action = PresenceAction.Leave,
                            ClientId = "4",
                            ConnectionId = "4",
                            Id = "4:2:2",
                            Data = string.Empty
                        },
                    };
                }

                bool seenLeaveMessageAsAbsentForClient4 = false;
                List<PresenceMessage> presenceMessagesLog = new List<PresenceMessage>();
                channel.Presence.Subscribe(message =>
                {
                    // keep track of the all presence messages we receive
                    presenceMessagesLog.Add(message);
                });

                channel.Presence.Subscribe(PresenceAction.Leave, async message =>
                {
                    /*
                        * Do not call it in states other than ATTACHED because of presence.get() side
                        * effect of attaching channel
                        */
                    if (message.ClientId == "4" && message.Action == PresenceAction.Leave && channel.State == ChannelState.Attached)
                    {
                        /*
                        * Client library won't return a presence message if it is stored as ABSENT
                        * so the result of the presence.get() call should be empty.
                        */
                        var result = await channel.Presence.GetAsync("4", waitForSync: false);
                        seenLeaveMessageAsAbsentForClient4 = result.ToArray().Length == 0;
                    }
                });

                await client.Connection.ConnectionManager.OnTransportMessageReceived(new ProtocolMessage()
                {
                    Action = ProtocolMessage.MessageAction.Sync,
                    Channel = channelName,
                    ChannelSerial = "1:1",
                    Presence = TestPresence1()
                });

                await client.Connection.ConnectionManager.OnTransportMessageReceived(new ProtocolMessage()
                {
                    Action = ProtocolMessage.MessageAction.Sync,
                    Channel = channelName,
                    ChannelSerial = "2:1",
                    Presence = TestPresence2()
                });

                await client.Connection.ConnectionManager.OnTransportMessageReceived(new ProtocolMessage()
                {
                    Action = ProtocolMessage.MessageAction.Sync,
                    Channel = channelName,
                    ChannelSerial = "2:",
                    Presence = TestPresence3()
                });

                var presence1 = await channel.Presence.GetAsync("1", false);
                var presence2 = await channel.Presence.GetAsync("2", false);
                var presence3 = await channel.Presence.GetAsync("3", false);
                var presenceOthers = await channel.Presence.GetAsync();

                presence1.ToArray().Length.ShouldBeEquivalentTo(0, "incomplete sync should be discarded");
                presence2.ToArray().Length.ShouldBeEquivalentTo(1, "client with id==2 should be in presence map");
                presence3.ToArray().Length.ShouldBeEquivalentTo(1, "client with id==3 should be in presence map");
                presenceOthers.ToArray().Length.ShouldBeEquivalentTo(2, "presence map should be empty");

                seenLeaveMessageAsAbsentForClient4.ShouldBeEquivalentTo(true, "LEAVE message for client with id==4 was not stored as ABSENT");

                PresenceMessage[] correctPresenceHistory = new PresenceMessage[]
                {
                    /* client 1 enters (will later be discarded) */
                    new PresenceMessage(PresenceAction.Enter, "1"),
                    /* client 2 enters */
                    new PresenceMessage(PresenceAction.Enter, "2"),
                    /* client 3 enters and never leaves because of newness comparison for LEAVE fails */
                    new PresenceMessage(PresenceAction.Enter, "3"),
                    /* client 4 enters and leaves */
                    new PresenceMessage(PresenceAction.Enter, "4"),
                    new PresenceMessage(PresenceAction.Leave, "4"), /* geting dupe */
                    /* client 1 is eliminated from the presence map because the first portion of SYNC is discarded */
                    new PresenceMessage(PresenceAction.Leave, "1")
                };

                presenceMessagesLog.Count.ShouldBeEquivalentTo(correctPresenceHistory.Length);

                for (int i = 0; i < correctPresenceHistory.Length; i++)
                {
                    PresenceMessage factualMsg = presenceMessagesLog[i];
                    PresenceMessage correctMsg = correctPresenceHistory[i];
                    factualMsg.ClientId.ShouldBeEquivalentTo(correctMsg.ClientId);
                    factualMsg.Action.ShouldBeEquivalentTo(correctMsg.Action);
                }
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP3")]
            public async Task Presence_AfterReconnectingShouldReattachChannelAndResumeBrokenSync(Protocol protocol)
            {
                var channelName = "RTP3".AddRandomSuffix();

                // must be greater than 100 to break up sync into multiple messages
                var enterCount = 150;

                var setupClient = await GetRealtimeClient(protocol);
                await setupClient.WaitForState(ConnectionState.Connected);

                // setup: enter clients on channel
                var testChannel = setupClient.Channels.Get(channelName);
                await testChannel.WaitForState(ChannelState.Attached);
                testChannel.Presence.Subscribe(PresenceAction.Enter, message => { });
                for (int i = 0; i < enterCount; i++)
                {
                    var clientId = $"fakeclient:{i}";
                    await testChannel.Presence.EnterClientAsync(clientId, $"RTP3 test entry {i}");
                }

                var client = await GetRealtimeClient(protocol, (options, _) =>
                {
                    Logger.LogLevel = LogLevel.Debug;
                });
                await client.WaitForState();

                var channel = client.Channels.Get(channelName);

                var syncMessageAwaiter = new TaskCompletionAwaiter();
                var transport = client.GetTestTransport();
                int syncCount = 0;
                transport.AfterDataReceived = protocolMessage =>
                {
                    if (protocolMessage.Action == ProtocolMessage.MessageAction.Sync)
                    {
                        syncCount++;

                        // interupt after first page of results
                        if (syncCount > 1)
                        {
                            transport.Close(false);
                            transport.AfterDataReceived = null;
                        }
                    }
                };

                channel.Attach();
                await channel.WaitForState(ChannelState.Attached);
                channel.State.Should().Be(ChannelState.Attached);

                await client.WaitForState(ConnectionState.Disconnected);
                client.Connection.State.Should().Be(ConnectionState.Disconnected);

                await client.WaitForState(ConnectionState.Connected);
                client.Connection.State.Should().Be(ConnectionState.Connected);

                await Task.Delay(500);

                var messages = await channel.Presence.GetAsync();
                var messageList = messages as IList<PresenceMessage> ?? messages.ToList();
                messageList.Count.ShouldBeEquivalentTo(enterCount, "Message count should match enterCount");

                setupClient.Close();
                client.Close();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP11")]
            [Trait("spec", "RTP11b")]
            [Trait("spec", "RTP11c")]
            [Trait("spec", "RTP11c1")]
            [Trait("spec", "RTP11c2")]
            [Trait("spec", "RTP11c3")]
            [Trait("spec", "RTP11d")]
            public async Task Presence_GetMethodBehaviour(Protocol protocol)
            {
                string channelName = "RTP11".AddRandomSuffix();
                var client1 = await GetRealtimeClient(protocol);
                var client2 = await GetRealtimeClient(protocol, (options, settings) => options.AutoConnect = false);

                var channel1 = client1.Channels.Get(channelName);
                await channel1.Presence.EnterClientAsync("1", "one");
                await channel1.Presence.EnterClientAsync("2", "two");

                var channel2 = client2.Channels.Get(channelName);
                var ch2Awaiter = new PresenceAwaiter(channel2);

                // with waitForSync set to false,
                // should result in 0 members because autoConnect is set to false
                var presenceMessages1 = await channel2.Presence.GetAsync(false);
                presenceMessages1.Should().HaveCount(0);

                client2.Connection.Connect();

                // With waitForSync is true it should get all the members entered on the first connection
                var presenceMessages2 = await channel2.Presence.GetAsync(true);
                presenceMessages2.Should().HaveCount(2);

                // enter third member from second connection
                await channel2.Presence.EnterClientAsync("3", null);

                // wait for the above to raise a subscribe event
                await ch2Awaiter.WaitFor(1);

                // filter by clientId
                var presenceMessages3 = await channel2.Presence.GetAsync("1");
                presenceMessages3.Should().HaveCount(1);
                presenceMessages3.First().ClientId.Should().Be("1");

                // filter by connectionId
                var presenceMessages4 = await channel2.Presence.GetAsync(connectionId: client2.Connection.Id);
                presenceMessages4.Should().HaveCount(1);
                presenceMessages4.First().ClientId.Should().Be("3");

                // filter by both clientId and connectionId
                var presenceMessages5 = await channel2.Presence.GetAsync(connectionId: client1.Connection.Id, clientId: "2");
                var presenceMessages6 = await channel2.Presence.GetAsync(connectionId: client2.Connection.Id, clientId: "2");
                presenceMessages5.Should().HaveCount(1);
                presenceMessages6.Should().HaveCount(0);
                presenceMessages5.First().ClientId.Should().Be("2");

                // become SUSPENDED
                await client2.ConnectionManager.SetState(new ConnectionSuspendedState(client2.ConnectionManager, Logger));
                await client2.WaitForState(ConnectionState.Suspended);

                // with waitForSync set to false, should get all the three members
                var presenceMessages7 = await channel2.Presence.GetAsync(false);
                presenceMessages7.Should().HaveCount(3);

                // with waitForSync set to true, should get exception
                client2.Connection.State.Should().Be(ConnectionState.Suspended);
                try
                {
                    await channel2.Presence.GetAsync(true);
                    throw new Exception("waitForSync=true shouldn't succeed in SUSPENDED state");
                }
                catch (AblyException e)
                {
                    e.ErrorInfo.Code.Should().Be(91005);
                }
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP19")]
            public async Task PresenceMap_WithExistingMembers_WhenSync_ShouldRemoveLocalMembers_RTP19(Protocol protocol)
            {
                var channelName = "RTP19".AddRandomSuffix();
                var client = await GetRealtimeClient(protocol);

                var channel = client.Channels.Get(channelName);
                await channel.Presence.EnterClientAsync("1", "one");
                await channel.Presence.EnterClientAsync("2", "two");

                channel.Presence.Map.Members.Should().HaveCount(2);

                var localMessage = new PresenceMessage()
                {
                    Action = PresenceAction.Enter,
                    Id = $"local:0:0",
                    Timestamp = DateTimeOffset.UtcNow,
                    ClientId = "local".AddRandomSuffix(),
                    ConnectionId = "local",
                    Data = "local data"
                };

                channel.Presence.Map.Members[localMessage.MemberKey] = localMessage;
                channel.Presence.Map.Members.Should().HaveCount(3);
                channel.Presence.Map.Members.ContainsKey(localMessage.MemberKey).Should().BeTrue();

                var members = (await channel.Presence.GetAsync()).ToArray();
                members.Should().HaveCount(3);
                members.Where(m => m.ClientId == "1").Should().HaveCount(1);

                var leaveMessages = new List<PresenceMessage>();
                await WaitFor(async done =>
                 {
                     channel.Presence.Subscribe(PresenceAction.Leave, message =>
                     {
                         leaveMessages.Add(message);
                         done();
                     });

                     var msg = new ProtocolMessage
                     {
                         Action = ProtocolMessage.MessageAction.Sync,
                         Channel = channelName
                     };

                     await client.FakeProtocolMessageReceived(msg);
                 });

                leaveMessages.Should().HaveCount(1);
                leaveMessages[0].ClientId.Should().Be(localMessage.ClientId);

                members = (await channel.Presence.GetAsync()).ToArray();
                members.Should().HaveCount(2);
                members.Any(m => m.ClientId == localMessage.ClientId).Should().BeFalse();
            }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RTP19a")]
            [Trait("spec", "RTP6b")]
            public async Task PresenceMap_WithExistingMembers_WhenBecomesAttachedWithoutHasPresence_ShouldEmitLeavesForExistingMembers(Protocol protocol)
            {
                var channelName = "RTP19a".AddRandomSuffix();
                var client = await GetRealtimeClient(protocol);
                await client.WaitForState();
                var channel = client.Channels.Get(channelName);

                var localMessage1 = new PresenceMessage()
                {
                    Action = PresenceAction.Enter,
                    Id = $"local:0:1",
                    Timestamp = DateTimeOffset.UtcNow,
                    ClientId = "local".AddRandomSuffix(),
                    ConnectionId = "local",
                    Data = "local data 1"
                };

                var localMessage2 = new PresenceMessage()
                {
                    Action = PresenceAction.Enter,
                    Id = $"local:0:2",
                    Timestamp = DateTimeOffset.UtcNow,
                    ClientId = "local".AddRandomSuffix(),
                    ConnectionId = "local",
                    Data = "local data 2"
                };

                channel.Presence.Map.Members[localMessage1.MemberKey] = localMessage1;
                channel.Presence.Map.Members[localMessage2.MemberKey] = localMessage2;
                channel.Presence.Map.Members.Should().HaveCount(2);

                bool hasPresence = true;
                int leaveCount = 0;
                await WaitForMultiple(4, partialDone =>
                {
                    client.GetTestTransport().AfterDataReceived += message =>
                    {
                        if (message.Action == ProtocolMessage.MessageAction.Attached)
                        {
                            hasPresence = message.HasFlag(ProtocolMessage.Flag.HasPresence);
                            partialDone();
                        }
                    };

                    // (RTP6b) Subscribe with a single action argument
                    channel.Presence.Subscribe(PresenceAction.Leave, leaveMsg =>
                    {
                        leaveMsg.ClientId.Should().StartWith("local");
                        leaveMsg.Action.Should().Be(PresenceAction.Leave);
                        leaveMsg.Timestamp.Should().BeCloseTo(DateTime.UtcNow, 200);
                        leaveMsg.Id.Should().BeNull();
                        leaveCount++;
                        partialDone(); // should be called twice
                    });

                    channel.Attach((b, info) =>
                    {
                        b.Should().BeTrue();
                        info.Should().BeNull();
                        partialDone();
                    });
                });

                hasPresence.Should().BeFalse();
                leaveCount.Should().Be(2);

                var members = await channel.Presence.GetAsync();
                members.Should().HaveCount(0);
            }

            [Theory]
            [ProtocolData]
            public async Task WillThrowAblyException_WhenInvalidMessagesArePresent(Protocol protocol)
            {
                Logger.LogLevel = LogLevel.Debug;

                var channelName = "presence_tests_exception".AddRandomSuffix();

                var client = await GetRealtimeClient(protocol);
                await client.WaitForState(ConnectionState.Connected);
                client.Connection.State.ShouldBeEquivalentTo(ConnectionState.Connected);

                var channel = client.Channels.Get(channelName);
                channel.Attach();
                await channel.WaitForState(ChannelState.Attached);
                channel.State.ShouldBeEquivalentTo(ChannelState.Attached);

                PresenceMessage[] TestPresence1()
                {
                    return new PresenceMessage[]
                    {
                        new PresenceMessage
                        {
                            Action = PresenceAction.Enter,
                            ClientId = "2",
                            ConnectionId = "2",
                            Id = "2:1:0",
                            Data = string.Empty
                        },
                        new PresenceMessage
                        {
                            Action = PresenceAction.Enter,
                            ClientId = "2",
                            ConnectionId = "2",
                            Id = "2:1:SHOULD_ERROR",
                            Data = string.Empty
                        },
                    };
                }

                bool caught = false;
                try
                {
                    channel.Presence.OnPresence(TestPresence1(), "xyz");
                }
                catch (Exception ex)
                {
                    ex.Should().BeOfType<AblyException>();
                    caught = true;
                }

                caught.Should().BeTrue();
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
                    ResetEvent.Set();
                });

                await channel.Presence.EnterAsync(new[] { "test", "best" });

                ResetEvent.WaitOne(2000);
                time.Should().HaveValue();
            }

            [Fact]
            public void PresenceMessage_IsNewerThan_RaisesExceptionWithMalformedIdStrings()
            {
                // Set the first portion of the Id to match the connection id
                // This is so the message does not appear synthesised
                var p1 = new PresenceMessage(PresenceAction.Present, "client1");
                p1.Id = "abcdef:1:1";
                p1.ConnectionId = "abcdef";
                p1.Timestamp = new DateTimeOffset(2000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero);
                var p2 = new PresenceMessage(PresenceAction.Present, "client2");

                // make the timestamps the same so we can get to the parsing part of the code
                p2.Timestamp = p1.Timestamp;
                p2.Id = "abcdef:this_should:error";
                p2.ConnectionId = "abcdef";

                bool exHandled = false;
                try
                {
                    p1.IsNewerThan(p2);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<AblyException>();
                    e.Message.Should().Contain("this_should:error");
                    exHandled = true;
                }

                exHandled.Should().BeTrue();
                exHandled = false;

                try
                {
                    p2.IsNewerThan(p1);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<AblyException>();
                    e.Message.Should().Contain("this_should:error");
                    exHandled = true;
                }

                exHandled.Should().BeTrue();
                exHandled = false;

                // test not enough sections
                p2.Id = "abcdef:2";
                try
                {
                    p1.IsNewerThan(p2);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<AblyException>();
                    e.Message.Should().Contain("abcdef:2");
                    exHandled = true;
                }

                exHandled.Should().BeTrue();
                exHandled = false;

                try
                {
                    p2.IsNewerThan(p1);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<AblyException>();
                    e.Message.Should().Contain("abcdef:2");
                    exHandled = true;
                }

                exHandled.Should().BeTrue();
                exHandled = false;

                // correctly formatted Ids should not throw an exception
                p2.Id = p1.Id;
                try
                {
                    p2.IsNewerThan(p1);
                }
                catch (Exception)
                {
                    exHandled = true;
                }

                exHandled.Should().BeFalse();
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

                // enters 250 members on a single connection A
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
                var awaiter = new TaskCompletionAwaiter(200000);
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
                var messages = await channelB.Presence.GetAsync(new Presence.GetParams { WaitForSync = true });
                var messageList = messages as IList<PresenceMessage> ?? messages.ToList();
                messageList.Count().ShouldBeEquivalentTo(ExpectedEnterCount);
                foreach (var m in messageList)
                {
                    presenceMessages.Select(x => x.ClientId == m.ClientId).Any().Should().BeTrue();
                }

                clientA.Close();
                clientB.Close();
            }

            private string GetClientId(int count)
            {
                return "client:#" + count.ToString().PadLeft(3, '0');
            }

            public With250PresentMembersOnAChannel(AblySandboxFixture fixture, ITestOutputHelper output)
                : base(fixture, output)
            {
            }
        }

        public PresenceSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        protected string GetTestChannelName(string id = "")
        {
            return $"presence-{id}".AddRandomSuffix();
        }

        public class PresenceAwaiter
        {
            private IRealtimeChannel _channel;
            private TaskCompletionAwaiter _tsc;
            private int _count = 0;

            public PresenceAwaiter(IRealtimeChannel channel)
            {
                _channel = channel;
                _channel.Presence.Subscribe(message =>
                {
                    _tsc?.Tick();
                });
            }

            public async Task<bool> WaitFor(int count)
            {
                _count = count;
                _tsc = new TaskCompletionAwaiter(10000, count);
                return await _tsc.Task;
            }
        }
    }
}
