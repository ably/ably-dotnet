using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ChannelSpecs : ConnectionSpecsBase
    {
        public class GeneralSpecs : ChannelSpecs
        {
            [Theory]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Failed)]
            [Trait("spec", "RTL11")]
            public void WhenDetachedOrFailed_AllQueuedMessagesShouldBeDeletedAndFailCallbackInvoked(ChannelState state)
            {
                var client = GetConnectedClient();
                var channel = client.Channels.Get("test");
                var expectedError = new ErrorInfo();
                channel.Attach();

                channel.Publish("test", "data", (success, error) =>
                {
                    success.Should().BeFalse();
                    error.Should().BeSameAs(expectedError);
                });

                var realtimeChannel = (channel as RealtimeChannel);
                realtimeChannel.QueuedMessages.Should().HaveCount(1);
                realtimeChannel.SetChannelState(state, expectedError);
                realtimeChannel.QueuedMessages.Should().HaveCount(0);
            }

            [Fact]
            [Trait("spec", "RTL9")]
            [Trait("spec", "RTL9a")]
            public void ChannelPresenceShouldReturnAPresenceObject()
            {
                var client = GetConnectedClient();
                var channel = client.Get("Test");
                channel.Presence.Should().BeOfType<Presence>();
            }

            public GeneralSpecs(ITestOutputHelper output) : base(output)
            {
            }
        }

        [Trait("spec", "RTL2")]
        public class EventEmitterSpecs : ChannelSpecs
        {
            private IRealtimeChannel _channel;

            [Theory]
            [InlineData(ChannelState.Initialized)]
            [InlineData(ChannelState.Attaching)]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Detaching)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Failed)]
            [Trait("spec", "RTL2a")]
            [Trait("spec", "RTL2b")]
            public void ShouldEmitTheFollowingStates(ChannelState state)
            {
                _channel.ChannelStateChanged += (sender, args) =>
                {
                    args.NewState.Should().Be(state);
                    _channel.State.Should().Be(state);
                };

                (_channel as RealtimeChannel).SetChannelState(state);
            }

            [Fact]
            [Trait("spec", "RTL2c")]
            public void ShouldEmmitErrorWithTheErrorThatHasOccuredOnTheChannel()
            {
                var error = new ErrorInfo();
                _channel.ChannelStateChanged += (sender, args) =>
                {
                    args.Reason.Should().BeSameAs(error);
                    _channel.Reason.Should().BeSameAs(error);
                };

                (_channel as RealtimeChannel).SetChannelState(ChannelState.Attached, error);
            }

            public EventEmitterSpecs(ITestOutputHelper output) : base(output)
            {
                _channel = GetConnectedClient().Channels.Get("test");
            }
        }

        [Trait("spec", "RTL3")]
        public class ConnectionStateChangeEffectSpecs : ChannelSpecs
        {
            private AblyRealtime _client;
            private IRealtimeChannel _channel;

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3a")]
            public async Task WhenConnectionFails_AttachingOrAttachedChannelsShouldTrasitionToFailedWithSameError(ChannelState state)
            {
                var error = new ErrorInfo();
                (_channel as RealtimeChannel).SetChannelState(state);
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = error });

                _client.Connection.State.Should().Be(ConnectionStateType.Failed);
                _channel.State.Should().Be(ChannelState.Failed);
                _channel.Reason.Should().BeSameAs(error);
            }

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3b")]
            public async Task WhenConnectionIsClosed_AttachingOrAttachedChannelsShouldTrasitionToDetached(ChannelState state)
            {
                (_channel as RealtimeChannel).SetChannelState(state);

                _client.Close();

                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

                _client.Connection.State.Should().Be(ConnectionStateType.Closed);
                _channel.State.Should().Be(ChannelState.Detached);
            }

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3b")]
            public async Task WhenConnectionIsSuspended_AttachingOrAttachedChannelsShouldTrasitionToDetached(ChannelState state)
            {
                (_channel as RealtimeChannel).SetChannelState(state);

                _client.Close();

                await _client.ConnectionManager.SetState(new ConnectionSuspendedState(_client.ConnectionManager));

                _client.Connection.State.Should().Be(ConnectionStateType.Suspended);
                _channel.State.Should().Be(ChannelState.Detached);
            }

            public ConnectionStateChangeEffectSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient();
                _channel = _client.Channels.Get("test");
            }
        }

     
        [Trait("spec", "RTL4")]
        public class ChannelAttachSpecs : ChannelSpecs
        {
            private AblyRealtime _client;
            private IRealtimeChannel _channel;

            [Theory]
            [InlineData(ChannelState.Attaching)]
            [InlineData(ChannelState.Attached)]
            [Trait("spec", "RTL4a")]
            public void WhenAttachingOrAttached_ShouldDoNothing(ChannelState state)
            {
                SetState(state);

                _channel.ChannelStateChanged += (sender, args) =>
                {
                    true.Should().BeFalse("This should not happen.");
                };

                _channel.Attach();
            }

            [Fact]
            [Trait("spec", "RTL4b")]
            public void WhenConnectionIsClosedClosingSuspendedOrFailed_ShouldThrowError()
            {
                //Closed
                _client.Connection.ConnectionState = new ConnectionClosedState(_client.ConnectionManager);
                Assert.Throws<AblyException>(() => _client.Get("closed").Attach());

                //Closing
                _client.Connection.ConnectionState = new ConnectionClosingState(_client.ConnectionManager);
                Assert.Throws<AblyException>(() => _client.Get("closing").Attach());

                //Suspended
                _client.Connection.ConnectionState = new ConnectionSuspendedState(_client.ConnectionManager);
                Assert.Throws<AblyException>(() => _client.Get("suspended").Attach());

                //Failed
                _client.Connection.ConnectionState = new ConnectionFailedState(_client.ConnectionManager, ErrorInfo.ReasonFailed);
                Assert.Throws<AblyException>(() => _client.Get("failed").Attach());
            }

            [Fact]
            [Trait("spec", "RTL4c")]
            public async Task ShouldSetStateToAttachingSendAnAttachMessageAndWaitForAttachedMessage()
            {
                _channel.Attach();
                _channel.State.Should().Be(ChannelState.Attaching);

                var lastMessageSend = LastCreatedTransport.LastMessageSend;
                lastMessageSend.action.Should().Be(ProtocolMessage.MessageAction.Attach);
                lastMessageSend.channel.Should().Be(_channel.Name);

                await ReceiveAttachedMessage();

                _channel.State.Should().Be(ChannelState.Attached);
            }



            [Fact]
            [Trait("spec", "RTL4f")]
            public async Task ShouldFailIfAttachMessageNotReceivedWithinDefaultTimeout()
            {
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                _channel.Attach();

                await Task.Delay(130);

                _channel.State.Should().Be(ChannelState.Failed);
                _channel.Reason.Should().NotBeNull();
            }

            [Fact]
            [Trait("spec", "RTL4d")]
            public async Task WithACallback_ShouldCallCallbackOnceAttached()
            {
                var called = false;
                _channel.Attach((span, info) =>
                {
                    called = true;
                    info.Should().BeNull();
                });

                await ReceiveAttachedMessage();

                called.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL4d")]
            public async Task WithACallback_ShouldCallCallbackWithErrorIfAttachFails()
            {
                Logger.LogLevel = LogLevel.Debug;
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                bool called = false;
                _channel.Attach((span, info) =>
                {
                    called = true;
                    info.Should().NotBeNull();
                });

                await Task.Delay(120);

                called.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL4d")]
            public async Task WhenCallingAsyncMethod_ShouldSucceedWhenChannelReachesAttached()
            {
                var attachTask = _channel.AttachAsync();
                await Task.WhenAll(attachTask, ReceiveAttachedMessage());

                attachTask.Result.IsSuccess.Should().BeTrue();
                attachTask.Result.Error.Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RTL4d")]
            public async Task WhenCallingAsyncMethod_ShouldFailWithErrorWhenAttachFails()
            {
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                var attachTask = _channel.AttachAsync();

                await Task.WhenAll(Task.Delay(120), attachTask);

                attachTask.Result.IsFailure.Should().BeTrue();
                attachTask.Result.Error.Should().NotBeNull();
            }

            private void SetState(ChannelState state, ErrorInfo error = null, ProtocolMessage message = null)
            {
                (_channel as RealtimeChannel).SetChannelState(state, error, message);
            }

            private async Task ReceiveAttachedMessage()
            {
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached)
                {
                    channel = _channel.Name
                });
            }

            public ChannelAttachSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient();
                _channel = _client.Channels.Get("test");
            }
        }

        [Trait("spec", "RTL5")]
        public class ChannelDetachSpecs : ChannelSpecs
        {
            private AblyRealtime _client;
            private IRealtimeChannel _channel;

            [Theory]
            [InlineData(ChannelState.Initialized)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Detaching)]
            [Trait("spec", "RTL5a")]
            public void WhenInitializedDetachedOrDetaching_ShouldDoNothing(ChannelState state)
            {
                SetState(state);
                bool changed = false;
                _channel.ChannelStateChanged += (sender, args) =>
                {
                    changed = true;
                };

                _channel.Detach();
                changed.Should().BeFalse();
            }

            [Fact]
            [Trait("spec", "RTL5b")]
            public void WhenStateIsFailed_DetachShouldThrowAnError()
            {
                SetState(ChannelState.Failed, new ErrorInfo());

                var ex = Assert.Throws<AblyException>(() => _channel.Detach());
            }

            [Fact]
            [Trait("spec", "RTL5d")]
            public async Task ShouldSendDetachMessageAndOnceDetachedReceviedShouldMigrateToDetached()
            {
                SetState(ChannelState.Attached);

                _channel.Detach();

                LastCreatedTransport.LastMessageSend.action.Should().Be(ProtocolMessage.MessageAction.Detach);
                _channel.State.Should().Be(ChannelState.Detaching);
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Detached) { channel = _channel.Name });

                _channel.State.Should().Be(ChannelState.Detached);
            }

            [Fact]
            [Trait("spec", "RTL5f")]
            public async Task ShouldFailIfAttachMessageNotReceivedWithinDefaultTimeout()
            {
                SetState(ChannelState.Attached);
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                _channel.Detach();

                await Task.Delay(130);

                _channel.State.Should().Be(ChannelState.Failed);
                _channel.Reason.Should().NotBeNull();
            }

            [Fact]
            [Trait("spec", "RTL5e")]
            public async Task WithACallback_ShouldCallCallbackOnceDetach()
            {
                SetState(ChannelState.Attached);

                var called = false;
                _channel.Detach((span, info) =>
                {
                    called = true;
                    info.Should().BeNull();
                });

                await ReceiveDetachedMessage();

                called.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL5e")]
            public async Task WithACallback_ShouldCallCallbackWithErrorIfDetachFails()
            {
                SetState(ChannelState.Attached);

                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                bool called = false;
                _channel.Detach((span, info) =>
                {
                    called = true;
                    info.Should().NotBeNull();
                });

                await Task.Delay(120);

                called.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL5e")]
            public async Task WhenCallingAsyncMethod_ShouldSucceedWhenChannelReachesDetached()
            {
                SetState(ChannelState.Attached);

                var detachTask = _channel.DetachAsync();
                await Task.WhenAll(detachTask, ReceiveDetachedMessage());

                detachTask.Result.IsSuccess.Should().BeTrue();
                detachTask.Result.Error.Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RTL5e")]
            public async Task WhenCallingAsyncMethod_ShouldFailWithErrorWhenDetachFails()
            {
                SetState(ChannelState.Attached);

                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                var detachTask = _channel.DetachAsync();

                await Task.WhenAll(Task.Delay(120), detachTask);

                detachTask.Result.IsFailure.Should().BeTrue();
                detachTask.Result.Error.Should().NotBeNull();
            }

            private async Task ReceiveDetachedMessage()
            {
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Detached)
                {
                    channel = _channel.Name
                });
            }

            private void SetState(ChannelState state, ErrorInfo error = null, ProtocolMessage message = null)
            {
                (_channel as RealtimeChannel).SetChannelState(state, error, message);
            }

            public ChannelDetachSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient();
                _channel = _client.Channels.Get("test");
            }
        }

        [Trait("spec", "RTL6")]
        public class PublishSpecs : ChannelSpecs
        {
            AblyRealtime _client;

            [Fact]
            [Trait("spec", "RTL6a")]
            [Trait("spec", "RTL6i")]
            [Trait("spec", "RTL6ia")]
            public void WithNameAndData_ShouldSendASingleProtocolMessageWithASingleEncodedMessageInside()
            {
                var channel = _client.Get("test");
                var bytes = new byte[] { 1, 2, 3, 4, 5 };

                SetState(channel, ChannelState.Attached);

                channel.Publish("byte", bytes);

                var sentMessage = LastCreatedTransport.LastMessageSend.messages.First();
                LastCreatedTransport.SentMessages.Should().HaveCount(1);
                sentMessage.encoding.Should().Be("base64");
            }

            [Fact]
            [Trait("spec", "RTL6i")]
            [Trait("spec", "RTL6i2")]
            public async Task WithListOfMessages_ShouldPublishASingleProtocolMessageToTransport()
            {
                var channel = _client.Get("test");
                SetState(channel, ChannelState.Attached);

                var list = new List<Message>
                {
                    new Message("name", "test"),
                    new Message("test", "best")
                };
                channel.Publish(list);

                LastCreatedTransport.SentMessages.Should().HaveCount(1);
                LastCreatedTransport.LastMessageSend.messages.Should().HaveCount(2);
            }

            [Fact]
            [Trait("spec", "RTL6i3")]
            public void WithMessageWithOutData_ShouldSendOnlyData()
            {
                var channel = _client.Get("test");
                SetState(channel, ChannelState.Attached);

                channel.Publish(null, "data");

                LastCreatedTransport.SentMessages.First().Text.Should().Contain("\"messages\":[{\"data\":\"data\"}]");
            }

            [Fact]
            [Trait("spec", "RTL6i3")]
            public void WithMessageWithOutName_ShouldSendOnlyData()
            {
                var channel = _client.Get("test");
                SetState(channel, ChannelState.Attached);

                channel.Publish("name", null);

                LastCreatedTransport.SentMessages.First().Text.Should().Contain("\"messages\":[{\"name\":\"name\"}]");
            }

            [Trait("spec", "RTL6c")]
            public class ConnectionStateConditions : PublishSpecs
            {
                [Fact]
                [Trait("spec", "RTL6c1")]
                public void WhenConnectionIsConnected_ShouldSendMessagesDirectly()
                {
                    var client = GetConnectedClient();

                    var channel = client.Channels.Get("test");
                    SetState(channel, ChannelState.Attached);

                    channel.Publish("test", "best");

                    var lastMessageSend = LastCreatedTransport.LastMessageSend;
                    lastMessageSend.channel.Should().Be("test");
                    lastMessageSend.messages.First().name.Should().Be("test");
                    lastMessageSend.messages.First().data.Should().Be("best");
                }

                [Fact]
                [Trait("spec", "RTL6c2")]
                public async Task WhenConnectionIsConnecting_MessageShouldBeQueuedUntilConnectionMovesToConnected()
                {
                    var client = GetClientWithFakeTransport();
                    client.Connect();
                    client.Connection.State.Should().Be(ConnectionStateType.Connecting);
                    var channel = client.Get("connecting");
                    SetState(channel, ChannelState.Attached);
                    channel.Publish("test", "connecting");

                    LastCreatedTransport.LastMessageSend.Should().BeNull();
                    client.ConnectionManager.PendingMessages.Should().HaveCount(1);
                    client.ConnectionManager.PendingMessages.First().Message.messages.First().data.Should().Be("connecting");

                    //Not connect the client
                    await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                    //Messages should be sent
                    LastCreatedTransport.LastMessageSend.Should().NotBeNull();
                    client.ConnectionManager.PendingMessages.Should().BeEmpty();
                }

                [Fact]
                [Trait("spec", "RTL6c2")]
                public async Task WhenConnectionIsDisconnecting_MessageShouldBeQueuedUntilConnectionMovesToConnected()
                {
                    var client = GetClientWithFakeTransport();
                    await client.ConnectionManager.SetState(new ConnectionDisconnectedState(client.ConnectionManager));
                    client.Connection.State.Should().Be(ConnectionStateType.Disconnected);
                    var channel = client.Get("connecting");
                    SetState(channel, ChannelState.Attached);
                    channel.Publish("test", "connecting");

                    LastCreatedTransport.LastMessageSend.Should().BeNull();
                    client.ConnectionManager.PendingMessages.Should().HaveCount(1);
                    client.ConnectionManager.PendingMessages.First().Message.messages.First().data.Should().Be("connecting");

                    //Now connect
                    client.Connect();
                    await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                    //Pending messages are sent
                    LastCreatedTransport.LastMessageSend.Should().NotBeNull();
                    client.ConnectionManager.PendingMessages.Should().BeEmpty();
                }



                public ConnectionStateConditions(ITestOutputHelper output) : base(output)
                {
                    //Inherit _client from parent    
                }
            }

            public class ClientIdSpecs : PublishSpecs
            {
                [Fact]
                [Trait("spec", "RTL6g1")]
                [Trait("spec", "RTL6g1a")]
                public void WithClientIdInOptions_DoesNotSetClientIdOnPublishedMessages()
                {
                    var client = GetConnectedClient(opts =>
                    {
                        opts.ClientId = "123";
                        opts.Token = "test";
                    });
                    var channel = client.Get("test");
                    channel.Publish("test", "test");
                    SetState(channel, ChannelState.Attached);

                    LastCreatedTransport.LastMessageSend.messages.First().clientId.Should().BeNullOrEmpty();
                }

                [Fact]
                [Trait("spec", "RTL6h")]
                public void CanEasilyAddClientIdWhenPublishingAMessage()
                {
                    var client = GetConnectedClient();
                    var channel = client.Get("test");
                    SetState(channel, ChannelState.Attached);
                    channel.Publish("test", "best", clientId: "123");

                    LastCreatedTransport.LastMessageSend.messages.First().clientId.Should().Be("123");
                }

                public ClientIdSpecs(ITestOutputHelper output) : base(output)
                {
                }
            }

            public PublishSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); //Easier to test encoding with the json protocol
            }
        }

        public class SubscribeSpecs : ChannelSpecs
        {
            private AblyRealtime _client;

            [Fact]
            [Trait("spec", "RTL7a")]
            public async Task WithNoArguments_AddsAListenerForAllMessages()
            {
                var channel = _client.Get("Test");
                SetState(channel, ChannelState.Attached);
                var messages = new List<Message>();
                channel.Subscribe(message =>
                {
                    messages.Add(message);
                });

                await _client.FakeMessageReceived(new Message("test", "best"), "Test");
                await _client.FakeMessageReceived(new Message("", "best"), "Test");
                await _client.FakeMessageReceived(new Message("blah", "best"), "Test");

                messages.Should().HaveCount(3);
            }

            [Fact]
            [Trait("spec", "RTL7b")]
            public async Task WithEventArguments_ShouldOnlyNotifyWhenNameMatchesMessageName()
            {
                var channel = _client.Get("test");
                SetState(channel, ChannelState.Attached);
                var messages = new List<Message>();
                channel.Subscribe("test", message =>
                {
                    messages.Add(message);
                });

                await _client.FakeMessageReceived(new Message("test", "best"), "test");
                await _client.FakeMessageReceived(new Message("", "best"), "test");
                await _client.FakeMessageReceived(new Message("blah", "best"), "test");

                messages.Should().HaveCount(1);
            }

            [Fact]
            [Trait("spec", "RTL7c")]
            public void ShouldImplicitlyAttachAChannel()
            {
                var channel = _client.Get("best");
                channel.State.Should().Be(ChannelState.Initialized);
                channel.Subscribe(message =>
                {
                    //do nothing
                });
                channel.State.Should().Be(ChannelState.Attaching);
            }

            [Fact]
            [Trait("spec", "RTL7d")]
            public async Task WithAMessageThatFailDecryption_ShouldDeliverMessageButEmmitErrorOnTheChannel()
            {
                var otherChannelOptions = new ChannelOptions(true);
                var encryptedChannel = _client.Get("encrypted", new ChannelOptions(true));
                SetState(encryptedChannel, ChannelState.Attached);
                bool msgReceived = false, 
                    errorEmitted = false;
                encryptedChannel.Subscribe(msg =>
                {
                    msgReceived = true;
                    msg.encoding.Should().Be("");
                });
                encryptedChannel.OnChannelError += (sender, args) =>
                {
                    errorEmitted = true;
                    args.Reason.Should().NotBeNull();
                };

                var message = new Message("name", "encrypted with otherChannelOptions");
                new MessageHandler(Protocol.Json).EncodePayloads(otherChannelOptions, new[] {message});
                await _client.FakeMessageReceived(message, encryptedChannel.Name);

                msgReceived.Should().BeTrue();
                errorEmitted.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL7e")]
            public async Task WithMessageThatCantBeDecoded_ShouldDeliverMessageWithResidualEncodingAndEmitTheErrorOnTheChannel()
            {
                var channel = _client.Get("test");
                SetState(channel, ChannelState.Attached);

                Message receivedMessage = null;
                channel.Subscribe(msg =>
                {
                    receivedMessage = msg;
                });

                ErrorInfo error = null;
                channel.OnChannelError += (sender, args) =>
                {
                    error = args.Reason;
                };

                var message = new Message("name", "encrypted with otherChannelOptions") { encoding = "json" };
                await _client.FakeMessageReceived(message, channel.Name);

                error.Should().NotBeNull();
                receivedMessage.encoding.Should().Be("json");
            }

            public SubscribeSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); //Easier to test encoding with the json protocol
            }
        }

        [Trait("spec", "RTL8")]
        public class UnsubscribeSpecs : ChannelSpecs
        {
            private AblyRealtime _client;
            private IRealtimeChannel _channel;
            private Action<Message> _handler;

            [Fact]
            [Trait("spec", "RTL8a")]
            public async Task ShouldReturnTrueAndRemoveHandlerFromSubscribers()
            {
                _channel.Subscribe(_handler);
                var result = _channel.Unsubscribe(_handler);
                result.Should().BeTrue();
                await _client.FakeMessageReceived(new Message("test", "best"), _channel.Name);

                //Handler should not throw
            }

            [Fact]
            [Trait("spec", "RTL8b")]
            public async Task WithEventName_ShouldUnsubscribeHandlerFromTheSpecifiedEvent()
            {
                _channel.Subscribe("test", _handler);
                var result = _channel.Unsubscribe("test", _handler);
                result.Should().BeTrue();
                await _client.FakeMessageReceived(new Message("test", "best"), _channel.Name);
                //Handler should not throw
            }

            public UnsubscribeSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); //Easier to test encoding with the json protocol
                _channel = _client.Get("test");

                SetState(_channel, ChannelState.Attached);
                _handler = message =>
                {
                    throw new AssertionFailedException("This handler should no longer be called");
                };
            }
        }

        [Trait("spec", "RTL10")]
        public class HistorySpecs : ChannelSpecs
        {
            private AblyRealtime _client;

            [Fact]
            public async Task ShouldCallRestClientToGetHistory()
            {
                var channel = _client.Get("history");

                await channel.History();

                Assert.Equal($"/channels/{channel.Name}/messages", LastRequest.Url);
            }

            [Fact]
            [Trait("spec", "RTL10b")]
            public async Task WithUntilAttach_ShouldPassAttachedSerialToHistoryQuery()
            {
                var channel = _client.Get("history");
                SetState(channel, ChannelState.Attached, message: new ProtocolMessage(ProtocolMessage.MessageAction.Attached) { channelSerial = "101"});

                await channel.History(untilAttached: true);

                LastRequest.QueryParameters.Should()
                    .ContainKey("fromSerial")
                    .WhichValue.Should().Be("101");
            }

            [Fact]
            public async Task WithUntilAttachButChannelNotAttached_ShouldThrowException()
            {
                var channel = _client.Get("history");

                var ex = await Assert.ThrowsAsync<AblyException>(() => channel.History(true));
            }





            public HistorySpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient();
            }
        }


        protected void SetState(IRealtimeChannel channel, ChannelState state, ErrorInfo error = null, ProtocolMessage message = null)
        {
            (channel as RealtimeChannel).SetChannelState(state, error, message);
        }

        public ChannelSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}