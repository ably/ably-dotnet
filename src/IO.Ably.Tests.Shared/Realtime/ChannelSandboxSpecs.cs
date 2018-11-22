using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Types;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("Channel SandBox")]
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
            // Arrange
            var client = await GetRealtimeClient(protocol);
            Semaphore signal = new Semaphore(0, 2);
            var args = new List<ChannelStateChange>();
            IRealtimeChannel target = client.Channels.Get("test");
            target.StateChanged += (s, e) =>
            {
                args.Add(e);
                signal.Release();
            };

            // Act
            target.Attach();

            // Assert
            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(1);
            args[0].Current.ShouldBeEquivalentTo(ChannelState.Attaching);
            args[0].Error.ShouldBeEquivalentTo(null);
            target.State.ShouldBeEquivalentTo(ChannelState.Attaching);

            signal.WaitOne(10000);
            args.Count.ShouldBeEquivalentTo(2);
            args[1].Current.ShouldBeEquivalentTo(ChannelState.Attached);
            args[1].Error.ShouldBeEquivalentTo(null);
            target.State.ShouldBeEquivalentTo(ChannelState.Attached);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL1")]
        public async Task SendingAMessageAttachesTheChannel_BeforeReceivingTheMessages(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;

            // Arrange
            var client = await GetRealtimeClient(protocol);
            IRealtimeChannel target = client.Channels.Get("test");
            var messagesReceived = new List<Message>();
            target.Subscribe(message =>
            {
                messagesReceived.Add(message);
                ResetEvent.Set();
            });

            // Act
            target.Publish("test", "test data");
            target.State.Should().Be(ChannelState.Attaching);
            ResetEvent.WaitOne(6000);

            // Assert
            target.State.Should().Be(ChannelState.Attached);
            messagesReceived.Count.ShouldBeEquivalentTo(1);
            messagesReceived[0].Name.ShouldBeEquivalentTo("test");
            messagesReceived[0].Data.ShouldBeEquivalentTo("test data");
        }

        // TODO: RTL1 Spec about presence and sync messages
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL4e")]
        public async Task WhenAttachingAChannelWithInsufficientPermissions_ShouldSetItToFailedWithError(
            Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.Key = settings.KeyWithChannelLimitations;
            });

            var channel = client.Channels.Get("nono_" + protocol);
            var result = await channel.AttachAsync();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(40160);
            result.Error.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTC1a")]
        public async Task TestAttachChannel_Sending3Messages_EchoesItBack(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;

            // Arrange
            var client = await GetRealtimeClient(protocol);
            await client.WaitForState(ConnectionState.Connected);

            var tsc = new TaskCompletionAwaiter(10000, 3);
            IRealtimeChannel target = client.Channels.Get("test" + protocol);
            target.Attach();
            await target.WaitForState(ChannelState.Attached);

            ConcurrentQueue<Message> messagesReceived = new ConcurrentQueue<Message>();
            int count = 0;
            target.Subscribe(message =>
            {
                messagesReceived.Enqueue(message);
                tsc.Tick();
            });

            // Act
            await target.PublishAsync("test1", "test 12");
            await target.PublishAsync("test2", "test 123");
            await target.PublishAsync("test3", "test 321");

            bool result = await tsc.Task;
            result.Should().BeTrue();

            // Assert
            messagesReceived.Should().HaveCount(3);
            var messages = messagesReceived.ToList();
            messages[0].Name.ShouldBeEquivalentTo("test1");
            messages[0].Data.ShouldBeEquivalentTo("test 12");
            messages[1].Name.ShouldBeEquivalentTo("test2");
            messages[1].Data.ShouldBeEquivalentTo("test 123");
            messages[2].Name.ShouldBeEquivalentTo("test3");
            messages[2].Data.ShouldBeEquivalentTo("test 321");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL7f")]
        [Trait("spec", "RTC1a")]
        public async Task TestAttachChannel_SendingMessage_Doesnt_EchoesItBack(Protocol protocol)
        {
            var channelName = "echo_off_test";

            // this should be logged in MsWebSocketTrasnport.CreateSocket
            var testLogger = new TestLogger("Connecting to web socket on url:");

            // Arrange
            var client = await GetRealtimeClient(protocol, (o, _) =>
            {
                o.EchoMessages = false;
                o.Logger = testLogger;
            });
            await client.WaitForState();
            client.Options.EchoMessages.Should().Be(false);
            testLogger.MessageSeen.Should().Be(true);
            testLogger.FullMessage.Contains("echo=false").Should().Be(true);

            var channel = client.Channels.Get(channelName);

            channel.Attach();

            List<Message> messagesReceived = new List<Message>();
            channel.Subscribe(message =>
            {
                messagesReceived.Add(message);
            });

            // Act
            await channel.PublishAsync(channelName, "test data");

            // Assert
            messagesReceived.Should().BeEmpty();
        }

        /*
         * An optional callback can be provided to the #publish method that is called when the message
         * is successfully delivered or upon failure with the appropriate ErrorInfo error.
         * A test should exist to publish lots of messages on a few connections
         * to ensure all message success callbacks are called for all messages published.
         */
        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6b")]
        public async Task With3ClientsAnd60MessagesAndCallbacks_ShouldExecuteAllCallbacks(Protocol protocol)
        {
            var channelName = "test".AddRandomSuffix();

            List<bool> successes1 = new List<bool>();
            List<bool> successes2 = new List<bool>();
            List<bool> successes3 = new List<bool>();

            bool retry = true;
            int tries = 3;
            while (retry)
            {
                var client1 = await GetRealtimeClient(protocol);
                var client2 = await GetRealtimeClient(protocol);
                var client3 = await GetRealtimeClient(protocol);

                var messages = new List<Message>();
                for (int i = 0; i < 20; i++)
                {
                    messages.Add(new Message("name" + i, "data" + i));
                }

                var awaiter = new TaskCountAwaiter(60);
                foreach (var message in messages)
                {
                    client1.Channels.Get(channelName).Publish(new[] { message }, (b, info) =>
                   {
                       successes1.Add(b);
                       awaiter.Tick();
                   });
                    client2.Channels.Get(channelName).Publish(new[] { message }, (b, info) =>
                    {
                        successes2.Add(b);
                        awaiter.Tick();
                    });
                    client3.Channels.Get(channelName).Publish(new[] { message }, (b, info) =>
                    {
                        successes3.Add(b);
                        awaiter.Tick();
                    });
                }

                await awaiter.Task;
                if ((successes1.Count == 20 && successes2.Count == 20 && successes3.Count == 20) || tries <= 0)
                {
                    retry = false;
                }

                tries--;
            }

            successes1.Should().HaveCount(20, "Should have 20 successful callback executed");
            successes2.Should().HaveCount(20, "Should have 20 successful callback executed");
            successes3.Should().HaveCount(20, "Should have 20 successful callback executed");
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6e")]
        [Trait("spec", "RTL6e1")]
        public async Task WithBasicAuthAndAMessageWithClientId_ShouldReturnTheMessageWithThatClientID(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            client.Connect();
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            bool messageReceived = false;
            channel.Subscribe(message =>
            {
                message.ClientId.Should().Be("123");
                messageReceived = true;
                ResetEvent.Set();
            });

            await channel.PublishAsync(new Message("test", "withClientId") { ClientId = "123" });

            ResetEvent.WaitOne(4000).Should().BeTrue("Operation timed out");

            messageReceived.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6g1b")]
        public async Task WithAClientIdInOptions_ShouldReceiveMessageWithCorrectClientID(Protocol protocol)
        {
            var clientId = (new Random().Next(1, 100000) % 1000).ToString();
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = clientId);

            client.Connect();
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            int messagesReceived = 0;
            string receivedClienId = string.Empty;
            channel.Subscribe(message =>
            {
                receivedClienId = message.ClientId;
                Interlocked.Increment(ref messagesReceived);
            });

            await channel.PublishAsync(new Message("test", "withClientId"));
            await Task.Delay(1000);
            messagesReceived.Should().BeGreaterThan(0);
            receivedClienId.Should().Be(clientId);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6g1b")]
        public async Task WithAnImplicitClientIdFromToken_ShouldReceiveMessageWithCorrectClientID(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var rest = await GetRestClient(protocol);
            var token = await rest.Auth.RequestTokenAsync(new TokenParams() { ClientId = "1000" });
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.TokenDetails = token);

            client.Connect();
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            bool messageReceived = false;
            channel.Subscribe(message =>
            {
                message.ClientId.Should().Be("1000");
                messageReceived = true;
            });

            await channel.PublishAsync(new Message("test", "withClientId"));
            await Task.Delay(100);
            messageReceived.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6g2")]
        public async Task WithAClientIdInOptionsAndMatchingClientIdInMessage_ShouldSendAndReceiveMessageWithCorrectClientID(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "999");

            client.Connect();
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            bool messageReceived = false;
            channel.Subscribe(message =>
            {
                message.ClientId.Should().Be("999");
                messageReceived = true;
                ResetEvent.Set();
            });

            await channel.PublishAsync(new Message("test", "data") { ClientId = "999" });
            ResetEvent.WaitOne(4000).Should().BeTrue("Timed out");

            messageReceived.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6g2")]
        public async Task WithAClientIdInOptionsAndDifferentClientIdInMessage_ShouldNotSendMessageAndResultInAnError(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "999");

            client.Connect();
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            bool messageReceived = false;
            channel.Subscribe(message =>
            {
                message.ClientId.Should().Be("999");
                messageReceived = true;
            });

            var result = await channel.PublishAsync(new Message("test", "data") { ClientId = "1000" });
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            messageReceived.Should().BeFalse();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6g4")]
        public async Task
            WhenPublishingMessageWithCompatibleClientIdBeforeClientIdHasBeenConfigured_ShouldPublishTheMessageSuccessfully(
            Protocol protocol)
        {
            var clientId = "client1";
            var rest = await GetRestClient(protocol);
            var realtimeClient = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.AuthCallback = async @params => await rest.Auth.RequestTokenAsync(new TokenParams() { ClientId = clientId });
            });

            var channelName = "test".AddRandomSuffix();
            var channel = realtimeClient.Channels.Get(channelName);
            bool messageReceived = false;
            channel.Subscribe(message =>
            {
                messageReceived = true;
                message.ClientId.Should().Be(clientId);
            });

            await channel.PublishAsync(new Message("test", "best") { ClientId = "client1" });

            // wait up to ten seconds
            for (var i = 0; i < 100; i++)
            {
                if (!messageReceived)
                {
                    await Task.Delay(100);
                }
                else
                {
                    break;
                }
            }

            messageReceived.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6g4")]
        [Trait("spec", "RTL6g3")]
        [Trait("spec", "RTL6h")] // Look at PublishAsync
        public async Task
            WhenPublishingMessageWithInCompatibleClientIdBeforeClientIdHasBeenConfigured_ShouldPublishTheMessageAndReturnErrorFromTheServerAllowingFurtherMessagesToBePublished(
            Protocol protocol)
        {
            var clientId = "client1";
            var rest = await GetRestClient(protocol);
            var realtimeClient = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.AuthCallback = async @params => await rest.Auth.RequestTokenAsync(new TokenParams() { ClientId = clientId });
            });
            var channelName = "test".AddRandomSuffix();
            var channel = realtimeClient.Channels.Get(channelName);
            int messageReceived = 0;
            channel.Subscribe(message =>
            {
                Interlocked.Add(ref messageReceived, 1);
                message.ClientId.Should().Be(clientId);
            });

            var result = await channel.PublishAsync("test", "best", "client2");
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();

            messageReceived.Should().Be(0);

            // Send a followup message
            var followupMessage = await channel.PublishAsync("followup", "message");
            followupMessage.IsSuccess.Should().BeTrue();
            await Task.Delay(100);
            messageReceived.Should().Be(1);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6f")]
        public async Task ConnectionIdShouldMatchThatOfThePublisher(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "999");
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            Message testMessage = null;
            channel.Subscribe(message =>
            {
                testMessage = message;
                ResetEvent.Set();
            });

            await channel.PublishAsync(new Message("test", "best"));

            ResetEvent.WaitOne();
            var connectionId = client.Connection.Id;
            testMessage.Should().NotBeNull();
            testMessage.ConnectionId.Should().Be(connectionId);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL6f")]
        public async Task PublishedMessagesShouldContainMessageIdsWhenReceived(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;

            var client = await GetRealtimeClient(protocol, (opts, _) => opts.ClientId = "999");
            var channelName = "test".AddRandomSuffix();
            var channel = client.Channels.Get(channelName);
            List<Message> testMessages = new List<Message>();
            channel.Subscribe(message =>
            {
                testMessages.Add(message);
                if (testMessages.Count == 5)
                {
                    ResetEvent.Set();
                }
            });
            var messages = new[]
            {
                new Message("test", "best"),
                new Message("test", "best"),
                new Message("test", "best"),
                new Message("test", "best"),
                new Message("test", "best")
            };

            await channel.PublishAsync(messages);

            ResetEvent.WaitOne();
            testMessages.Select(x => x.Id).Should().NotContainNulls();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL7c")]
        public async Task WhenSubscribingToAChannelWithInsufficientPermissions_ShouldSetItToFailedWithError(
            Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol, (options, settings) =>
            {
                options.Key = settings.KeyWithChannelLimitations;
            });

            var channel = client.Channels.Get("nono");
            channel.Subscribe(message =>
            {
                // do nothing
            });

            var result = await new ChannelAwaiter(channel, ChannelState.Failed).WaitAsync();
            await Task.Delay(100);
            result.IsSuccess.Should().BeTrue();

            var error = channel.ErrorReason;
            error.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        public static IEnumerable<object[]> FixtureData
        {
            get
            {
                if (Config.MsgPackEnabled)
                {
                    yield return new object[] { Defaults.Protocol, GetAes128FixtureData() };
                    yield return new object[] { Defaults.Protocol, GetAes256FixtureData() };
                }

                yield return new object[] { Protocol.Json, GetAes128FixtureData() };
                yield return new object[] { Protocol.Json, GetAes256FixtureData() };
            }
        }

        [Theory]
        [MemberData(nameof(FixtureData))]
        [Trait("spec", "RTL7d")]
        public async Task ShouldPublishAndReceiveFixtureData(Protocol protocol, JObject fixtureData)
        {
            Logger.LogLevel = LogLevel.Debug;
            var items = (JArray)fixtureData["items"];
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var client = await GetRealtimeClient(protocol);

            var channel = client.Channels.Get("persisted:test".AddRandomSuffix(), GetOptions(fixtureData));
            var count = 0;
            Message lastMessage = null;
            channel.Subscribe(message =>
            {
                lastMessage = message;
                resetEvent.Set();
            });

            foreach (var item in items)
            {
                var encoded = item["encoded"];
                var encoding = (string)encoded["encoding"];
                var decodedData = DecodeData((string)encoded["data"], encoding);
                await channel.PublishAsync((string)encoded["name"], decodedData);
                var result = resetEvent.WaitOne(10000);
                result.Should().BeTrue("Operation timed out");
                if (lastMessage.Data is byte[])
                {
                    (lastMessage.Data as byte[]).Should().BeEquivalentTo(decodedData as byte[], "Item number {0} data does not match decoded data", count);
                }
                else if (encoding == "json")
                {
                    JToken.DeepEquals((JToken)lastMessage.Data, (JToken)decodedData).Should().BeTrue("Item number {0} data does not match decoded data", count);
                }
                else
                {
                    lastMessage.Data.Should().Be(decodedData, "Item number {0} data does not match decoded data", count);
                }

                count++;
                resetEvent.Reset();
            }

            client.Close();
        }

        [Theory]
        [InlineData(ChannelState.Detached)]
        [InlineData(ChannelState.Failed)]
        [InlineData(ChannelState.Suspended)]
        [Trait("spec", "RTL11")]
        public async Task WhenChannelEntersDetachedFailedSuspendedState_ShouldDeleteQueuedMessageAndCallbackShouldIndicateError(ChannelState state)
        {
            var client = await GetRealtimeClient(Defaults.Protocol, (options, settings) =>
                {
                    // A bogus AuthUrl will cause connection to become disconnected
                    options.AuthUrl = new Uri("http://235424c24.fake:49999");

                    // speed up the AuthUrl failure
                    options.HttpMaxRetryCount = 1;
                    options.HttpMaxRetryDuration = TimeSpan.FromMilliseconds(100);
                });

            var channel = client.Channels.Get("test".AddRandomSuffix());

            var tsc = new TaskCompletionAwaiter(5000);
            client.Connection.Once(ConnectionEvent.Disconnected, change =>
            {
                // place a message on the queue
                channel.Publish("wibble", "wobble", (success, info) =>
                {
                    // expect an error
                    tsc.Set(!success);
                });

                // setting the state should cause the queued message to be removed
                // and the callback to indicate an error
                (channel as RealtimeChannel).SetChannelState(state);
            });

            var result = await tsc.Task;
            result.Should().BeTrue("publish should have failed");
        }

        [Theory]
        [InlineData(ChannelState.Detached)]
        [InlineData(ChannelState.Failed)]
        [InlineData(ChannelState.Suspended)]
        [Trait("spec", "RTL11a")]
        public async Task WhenChannelEntersDetachedFailedSuspendedState_MessagesAwaitingAckOrNackShouldNotBeAffected(ChannelState state)
        {
            var client = await GetRealtimeClient(Defaults.Protocol);
            var channel = client.Channels.Get("test".AddRandomSuffix());
            var tsc = new TaskCompletionAwaiter(5000);

            channel.Once(ChannelEvent.Attached, async x =>
            {
                channel.Publish("wibble", "wobble", (success, info) =>
                {
                    // message publish should succeed
                    tsc.Set(success);
                });

                client.Connection.Once(ConnectionEvent.Disconnected, change =>
                {
                    (channel as RealtimeChannel).SetChannelState(state);
                });

                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected)
                {
                    Error = new ErrorInfo("test", 40140)
                });
            });

            channel.Attach();

            var result = await tsc.Task;
            result.Should().BeTrue();
        }

        [Fact]
        [Trait("bug", "102")]
        public async Task WhenAttachingToAChannelFromMultipleThreads_ItShouldNotThrowAnError()
        {
            Logger.LogLevel = LogLevel.Debug;

            var client1 = await GetRealtimeClient(Protocol.Json);
            var channel = client1.Channels.Get("test".AddRandomSuffix());
            var task = Task.Run(() => channel.Attach());
            await task.ConfigureAwait(false);
            var task2 = Task.Run(() => channel.Attach());
            await task2.ConfigureAwait(false);

            await Task.WhenAll(task, task2);

            var result = await new ChannelAwaiter(channel, ChannelState.Attached).WaitAsync();
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RTL10d")]
        public async Task WithOneClientPublishingAnotherShouldBeAbleToRetrieveMessages(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;

            var client1 = await GetRealtimeClient(protocol);

            var channelName = "persisted:history".AddRandomSuffix();
            var channel = client1.Channels.Get(channelName);
            await channel.AttachAsync();
            var messages = Enumerable.Range(1, 10).Select(x => new Message("name:" + x, "value:" + x));
            await channel.PublishAsync(messages);

            await Task.Delay(2000);

            var client2 = await GetRealtimeClient(protocol);
            var historyChannel = client2.Channels.Get(channelName);
            var history = await historyChannel.HistoryAsync(new PaginatedRequestParams() { Direction = QueryDirection.Forwards });

            history.Should().BeOfType<PaginatedResult<Message>>();
            history.Items.Should().HaveCount(10);
            for (int i = 0; i < 10; i++)
            {
                var message = history.Items.ElementAt(i);
                message.Name.Should().Be("name:" + (i + 1));
                message.Data.ToString().Should().Be("value:" + (i + 1));
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("issue", "117")]
        public async Task AttachAwaitShouldtimeoutIfStateChanges(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var client1 = await GetRealtimeClient(protocol, (opts, _) =>
            {
                opts.AutoConnect = false;
                opts.RealtimeRequestTimeout = new TimeSpan(0, 0, 30);
            });

            var channelName = "test".AddRandomSuffix();
            var channel = client1.Channels.Get(channelName);

            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();
            client1.Connection.On(ConnectionEvent.Connected, async state =>
            {
                tsc.SetResult(true);
                client1.GetTestTransport().Close(false);
                var result = await channel.AttachAsync();
                result.Error.Should().NotBeNull();
                result.Error.Message.Should().Contain("Timeout exceeded");
            });
            client1.Connect();
            var didConnect = await tsc.Task;
            didConnect.ShouldBeEquivalentTo(true, "this indicates that the connection event was handled.");
        }

        [Theory]
        [ProtocolData]
        [Trait("issue", "104")]
        public async Task AttachWithMultipleConcurrentClientsShouldWork(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var clients = new List<IRealtimeClient>
            {
                await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false),
                await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false),
                await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false),
                await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false),
                await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false)
            };

            var channelName = "test".AddRandomSuffix();
            foreach (var client in clients)
            {
                client.Connect();
                await client.WaitForState();
                client.Channels.Get(channelName).Attach();
                await client.Channels.Get(channelName).WaitForState();
            }

            await Task.Delay(TimeSpan.FromSeconds(15));
            foreach (var client in clients)
            {
                client.Channels.Get(channelName).State.Should().Be(ChannelState.Attached);
            }
        }

        [Theory]
        [ProtocolData]
        [Trait("issue", "116")]
        public async Task FailureOfHistoryApiCallMeansChannelsNoLongerAttach(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var client = await GetRealtimeClient(protocol, (opts, _) => opts.AutoConnect = false);
            client.Connection.On(ConnectionEvent.Connected, async args =>
            {
                await client.Channels.Get("test")
                    .HistoryAsync(new PaginatedRequestParams() { Start = DateHelper.CreateDate(1969, 1, 1) });
            });

            var result = await client.Channels.Get("name").AttachAsync();

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
        }

        [Theory]
        [ProtocolData]
        public async Task WhenAttachAsyncCalledAfterSubscribe_ShouldWaitUntilChannelIsAttached(Protocol protocol)
        {
            var client = await GetRealtimeClient(protocol);

            var channel = client.Channels.Get("test".AddRandomSuffix());
            channel.Subscribe(message => { });
            await channel.AttachAsync();
            channel.State.Should().Be(ChannelState.Attached);
        }

        // [Theory]
        // [ProtocolData]
        // public async Task WhenAttachAsyncCalledAfterSubscribe_ShouldWaitUntilChannelIsAttached(Protocol protocol)
        // {
        //    var client = await GetRealtimeClient(protocol);

        // var channel = client.Channels.Get("test".AddRandomSuffix());
        //    channel.Subscribe(delegate (Message message) { });
        //    await channel.AttachAsync();
        //    channel.State.Should().Be(ChannelState.Attached);
        // }
        private static JObject GetAes128FixtureData()
        {
            return JObject.Parse(ResourceHelper.GetResource("crypto-data-128.json"));
        }

        private static JObject GetAes256FixtureData()
        {
            return JObject.Parse(ResourceHelper.GetResource("crypto-data-256.json"));
        }

        private ChannelOptions GetOptions(JObject data)
        {
            var key = (string)data["key"];
            var iv = (string)data["iv"];
            var cipherParams = new CipherParams("aes", key, CipherMode.CBC, iv);
            return new ChannelOptions(cipherParams);
        }

        private object DecodeData(string data, string encoding)
        {
            if (encoding == "json")
            {
                return JsonHelper.Deserialize(data);
            }

            if (encoding == "base64")
            {
                return data.FromBase64();
            }

            return data;
        }

        public ChannelSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }
}
