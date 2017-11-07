using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.AcceptanceTests;
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
                        Interlocked.Add(ref syncComplete, channel2.Presence.InternalSyncComplete ? 1: 0);
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


                #region test data

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
                
                #endregion

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
                        var result = await channel.Presence.GetAsync("4");
                        seenLeaveMessageAsAbsentForClient4 = result.ToArray().Length == 0;
                    }
                });
                
                await client.Connection.ConnectionManager.OnTransportMessageReceived(new ProtocolMessage()
                {
                    Action = Types.ProtocolMessage.MessageAction.Sync,
                    Channel = channelName,
                    ChannelSerial = "1:1",
                    Presence = TestPresence1()
                });

                await client.Connection.ConnectionManager.OnTransportMessageReceived(new ProtocolMessage()
                {
                    Action = Types.ProtocolMessage.MessageAction.Sync,
                    Channel = channelName,
                    ChannelSerial = "2:1",
                    Presence = TestPresence2()
                });

                await client.Connection.ConnectionManager.OnTransportMessageReceived(new ProtocolMessage()
                {
                    Action = Types.ProtocolMessage.MessageAction.Sync,
                    Channel = channelName,
                    ChannelSerial = "2:",
                    Presence = TestPresence3()

                });

                var presence1 = await channel.Presence.GetAsync("1");
                var presence2 = await channel.Presence.GetAsync("2");
                var presence3 = await channel.Presence.GetAsync("3");
                var presenceOthers = await channel.Presence.GetAsync();

                presence1.ToArray().Length.ShouldBeEquivalentTo(0, "incomplete sync should be discarded");
                presence2.ToArray().Length.ShouldBeEquivalentTo(1, "client with id==2 should be in presence map");
                presence3.ToArray().Length.ShouldBeEquivalentTo(1, "client with id==3 should be in presence map");
                presenceOthers.ToArray().Length.ShouldBeEquivalentTo(2, "presence map should be empty");

                seenLeaveMessageAsAbsentForClient4.ShouldBeEquivalentTo(true, "LEAVE message for client with id==4 was not stored as ABSENT");

                PresenceMessage[] correctPresenceHistory = new PresenceMessage[] {
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
                    _resetEvent.Set();
                });

                await channel.Presence.EnterAsync(new[] { "test", "best" });

                _resetEvent.WaitOne(2000);
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
                p1.Timestamp = new DateTimeOffset(2000,1,1,1,1,1,1, TimeSpan.Zero);
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
                catch (Exception e)
                {
                    exHandled = true;
                }
                exHandled.Should().BeFalse();
                exHandled = false;
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
 