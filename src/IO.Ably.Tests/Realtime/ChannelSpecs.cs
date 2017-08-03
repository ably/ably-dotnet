using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
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
                var channel = client.Channels.Get("Test");
                channel.Presence.Should().BeOfType<Presence>();
            }

            [Fact]
            [Trait("spec", "RTL12")]
            [Trait("intermittent", "true")]
            public async Task OnceAttachedWhenConsequentAttachMessageArriveWithError_ShouldEmitErrorOnChannelButNoStateChange()
            {
                var client = GetConnectedClient();
                var channel = client.Channels.Get("test");
                SetState(channel, ChannelState.Attached);

                ErrorInfo expectedError = new ErrorInfo();
                ErrorInfo error = null;
                channel.Error += (sender, args) =>
                {
                    error = args.Reason;
                };
                bool stateChanged = false;

                await Task.Delay(10); //Allow the notification thread to fire

                ChannelState newState = ChannelState.Initialized;
                channel.On((args) =>
                {
                    stateChanged = true;
                    newState = args.Current;
                });

                await
                    client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached)
                    {
                        Error = expectedError,
                        Channel = "test"
                    });

                await Task.Delay(10); //Allow the notification thread to fire

                error.Should().BeSameAs(expectedError);
                stateChanged.Should().BeFalse("State should not have changed but is now: " + newState);
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
            public async Task ShouldEmitTheFollowingStates(ChannelState state)
            {
                ChannelState newState = ChannelState.Initialized;
                _channel.On(x =>
                {
                    newState = x.Current;
                    Done();
                });

                (_channel as RealtimeChannel).SetChannelState(state);
                
                WaitOne();

                _channel.State.Should().Be(state);
                newState.Should().Be(state);
            }

            [Fact]
            [Trait("spec", "RTL2c")]
            public async Task ShouldEmmitErrorWithTheErrorThatHasOccuredOnTheChannel()
            {
                var error = new ErrorInfo();
                ErrorInfo expectedError = null;
                _channel.On((args) =>
                {
                    expectedError = args.Error;
                    Done();
                });

                (_channel as RealtimeChannel).SetChannelState(ChannelState.Attached, error);

                WaitOne();

                expectedError.Should().BeSameAs(error);
                _channel.ErrorReason.Should().BeSameAs(error);

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
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = error });

                _client.Connection.State.Should().Be(ConnectionState.Failed);
                _channel.State.Should().Be(ChannelState.Failed);
                _channel.ErrorReason.Should().BeSameAs(error);
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

                _client.Connection.State.Should().Be(ConnectionState.Closed);
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

                _client.Connection.State.Should().Be(ConnectionState.Suspended);
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
            [InlineData(ChannelState.Attached)]
            [Trait("spec", "RTL4a")]
            public async Task WhenAttached_ShouldDoNothing(ChannelState state)
            {
                await SetState(state);
                bool stateChanged = false;
                ChannelState newState = ChannelState.Initialized;

                _channel.On((args) =>
                {
                    newState = args.Current;
                    stateChanged = true;
                });

                _channel.Attach();

                await Task.Delay(100);

                stateChanged.Should().BeFalse("This should not happen. State changed to: " + newState);
            }

            [Fact]
            [Trait("spec", "RTL4h")]
            public async Task WhenAttaching_ShouldAddMultipleAwaitingHandlers()
            {
                await SetState(ChannelState.Attaching);
                bool stateChanged = false;
                ChannelState newState = ChannelState.Initialized;
                int counter = 0;

                _channel.Attach((b, info) => counter++);
                _channel.Attach((b, info) => counter++);

                await ReceiveAttachedMessage();

                counter.Should().Be(2);
            }



            [Fact]
            [Trait("spec", "RTL4b")]
            public void WhenConnectionIsClosedClosingSuspendedOrFailed_ShouldThrowError()
            {
                //Closed
                _client.Connection.ConnectionState = new ConnectionClosedState(_client.ConnectionManager);
                Assert.Throws<AblyException>(() => _client.Channels.Get("closed").Attach());

                //Closing
                _client.Connection.ConnectionState = new ConnectionClosingState(_client.ConnectionManager);
                Assert.Throws<AblyException>(() => _client.Channels.Get("closing").Attach());

                //Suspended
                _client.Connection.ConnectionState = new ConnectionSuspendedState(_client.ConnectionManager);
                Assert.Throws<AblyException>(() => _client.Channels.Get("suspended").Attach());

                //Failed
                _client.Connection.ConnectionState = new ConnectionFailedState(_client.ConnectionManager, ErrorInfo.ReasonFailed);
                Assert.Throws<AblyException>(() => _client.Channels.Get("failed").Attach());
            }

            [Fact]
            [Trait("spec", "RTL4c")]
            public async Task ShouldSetStateToAttachingSendAnAttachMessageAndWaitForAttachedMessage()
            {
                _channel.Attach();
                _channel.State.Should().Be(ChannelState.Attaching);

                var lastMessageSend = LastCreatedTransport.LastMessageSend;
                lastMessageSend.Action.Should().Be(ProtocolMessage.MessageAction.Attach);
                lastMessageSend.Channel.Should().Be(_channel.Name);

                await ReceiveAttachedMessage();

                _channel.State.Should().Be(ChannelState.Attached);
            }

            [Theory]
            [InlineData(ChannelState.Initialized)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Detaching)]
            [InlineData(ChannelState.Failed)]
            [Trait("spec", "RTL4f")]
            public async Task ShouldReturnToPreviousStateIfAttachMessageNotReceivedWithinDefaultTimeout(ChannelState previousState)
            {
                SetState(_channel, previousState);

                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                _channel.Attach();

                await Task.Delay(150);


                _channel.State.Should().Be(previousState);
                _channel.ErrorReason.Should().NotBeNull();
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
                    Done();
                });

                await ReceiveAttachedMessage();

                WaitOne();

                called.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL4d")]
            public async Task WithACallback_ShouldCallCallbackWithErrorIfAttachFails()
            {
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                bool called = false;
                _channel.Attach((span, info) =>
                {
                    called = true;
                    info.Should().NotBeNull();
                    Done();
                });

                WaitOne();

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

            private async Task SetState(ChannelState state, ErrorInfo error = null, ProtocolMessage message = null)
            {
                (_channel as RealtimeChannel).SetChannelState(state, error, message);
                await Task.Delay(10);
            }

            private async Task ReceiveAttachedMessage()
            {
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached)
                {
                    Channel = _channel.Name
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

                _channel.On((args) =>
                {
                    changed = true;
                });

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

                LastCreatedTransport.LastMessageSend.Action.Should().Be(ProtocolMessage.MessageAction.Detach);
                _channel.State.Should().Be(ChannelState.Detaching);
                await _client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Detached) { Channel = _channel.Name });

                _channel.State.Should().Be(ChannelState.Detached);
            }

            [Fact]
            [Trait("spec", "RTL5f")]
            public async Task ShouldReturnToPreviousStateIfDetachedMessageWasNotReceivedWithinDefaultTimeout()
            {
                SetState(ChannelState.Attached);
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                bool detachSuccess = true;
                ErrorInfo detachError = null;
                _channel.Detach((success, error) =>
                {
                    detachSuccess = success;
                    detachError = error;
                });

                await Task.Delay(150);

                _channel.State.Should().Be(ChannelState.Attached);
                _channel.ErrorReason.Should().NotBeNull();
                detachSuccess.Should().BeFalse();
                detachError.Should().NotBeNull();
            }

            [Fact]
            [Trait("spec", "RTL5e")]
            public async Task WithACallback_ShouldCallCallbackOnceDetach()
            {
                SetState(ChannelState.Attached);

                bool called = false;
                var detached = false;
                _channel.Detach((span, info) =>
                {
                    called = true;
                    Assert.Null(info);
                    Done();
                });

                await ReceiveDetachedMessage();

                WaitOne();

                Assert.True(called);
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
                    Done();
                });

                WaitOne();

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
                    Channel = _channel.Name
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
                var channel = _client.Channels.Get("test");
                var bytes = new byte[] { 1, 2, 3, 4, 5 };

                SetState(channel, ChannelState.Attached);

                channel.PublishAsync("byte", bytes);

                var sentMessage = LastCreatedTransport.LastMessageSend.Messages.First();
                LastCreatedTransport.SentMessages.Should().HaveCount(1);
                sentMessage.Encoding.Should().Be("base64");
            }

            [Fact]
            [Trait("spec", "RTL6i")]
            [Trait("spec", "RTL6i2")]
            public void WithListOfMessages_ShouldPublishASingleProtocolMessageToTransport()
            {
                var channel = _client.Channels.Get("test");
                SetState(channel, ChannelState.Attached);

                var list = new List<Message>
                {
                    new Message("name", "test"),
                    new Message("test", "best")
                };

                channel.Publish(list);

                LastCreatedTransport.SentMessages.Should().HaveCount(1);
                LastCreatedTransport.LastMessageSend.Messages.Should().HaveCount(2);
            }

            [Fact]
            [Trait("spec", "RTL6i3")]
            public void WithMessageWithOutData_ShouldSendOnlyData()
            {
                var channel = _client.Channels.Get("test");
                SetState(channel, ChannelState.Attached);

                channel.Publish(null, "data");

                LastCreatedTransport.SentMessages.First().Text.Should().Contain("\"messages\":[{\"data\":\"data\"}]");
            }

            [Fact]
            [Trait("spec", "RTL6i3")]
            public void WithMessageWithOutName_ShouldSendOnlyData()
            {
                var channel = _client.Channels.Get("test");
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
                    lastMessageSend.Channel.Should().Be("test");
                    lastMessageSend.Messages.First().Name.Should().Be("test");
                    lastMessageSend.Messages.First().Data.Should().Be("best");
                }

                [Fact]
                [Trait("spec", "RTL6c2")]
                public async Task WhenConnectionIsConnecting_MessageShouldBeQueuedUntilConnectionMovesToConnected()
                {
                    var client = GetClientWithFakeTransport();
                    client.Connect();
                    client.Connection.State.Should().Be(ConnectionState.Connecting);
                    var channel = client.Channels.Get("connecting");
                    SetState(channel, ChannelState.Attached);
                    channel.Publish("test", "connecting");

                    LastCreatedTransport.LastMessageSend.Should().BeNull();
                    client.ConnectionManager.PendingMessages.Should().HaveCount(1);
                    client.ConnectionManager.PendingMessages.First().Message.Messages.First().Data.Should().Be("connecting");

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
                    client.Connection.State.Should().Be(ConnectionState.Disconnected);
                    var channel = client.Channels.Get("connecting");
                    SetState(channel, ChannelState.Attached);
                    channel.Publish("test", "connecting");

                    LastCreatedTransport.LastMessageSend.Should().BeNull();
                    client.ConnectionManager.PendingMessages.Should().HaveCount(1);
                    client.ConnectionManager.PendingMessages.First().Message.Messages.First().Data.Should().Be("connecting");

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
                    var channel = client.Channels.Get("test");
                    channel.PublishAsync("test", "test");
                    SetState(channel, ChannelState.Attached);

                    LastCreatedTransport.LastMessageSend.Messages.First().ClientId.Should().BeNullOrEmpty();
                }

                [Fact]
                [Trait("spec", "RTL6h")]
                public void CanEasilyAddClientIdWhenPublishingAMessage()
                {
                    var client = GetConnectedClient();
                    var channel = client.Channels.Get("test");
                    SetState(channel, ChannelState.Attached);
                    channel.PublishAsync("test", "best", clientId: "123");

                    LastCreatedTransport.LastMessageSend.Messages.First().ClientId.Should().Be("123");
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
                var channel = _client.Channels.Get("Test");
                SetState(channel, ChannelState.Attached);
                var messages = new List<Message>();
                int count = 0;
                channel.Subscribe(message =>
                {
                    messages.Add(message);
                    count++;
                    if (count == 2)
                        Done();
                });

                await _client.FakeMessageReceived(new Message("test", "best"), "Test");
                await _client.FakeMessageReceived(new Message("", "best"), "Test");
                await _client.FakeMessageReceived(new Message("blah", "best"), "Test");

                WaitOne();

                messages.Should().HaveCount(3);
            }

            [Fact]
            [Trait("spec", "RTL7b")]
            public async Task WithEventArguments_ShouldOnlyNotifyWhenNameMatchesMessageName()
            {
                var channel = _client.Channels.Get("test");
                SetState(channel, ChannelState.Attached);
                var messages = new List<Message>();
                channel.Subscribe("test", message =>
                {
                    messages.Add(message);
                    Done();
                });

                await _client.FakeMessageReceived(new Message("test", "best"), "test");
                await _client.FakeMessageReceived(new Message("", "best"), "test");
                await _client.FakeMessageReceived(new Message("blah", "best"), "test");

                WaitOne();

                messages.Should().HaveCount(1);
            }

            [Fact]
            [Trait("spec", "RTL7c")]
            public void ShouldImplicitlyAttachAChannel()
            {
                var channel = _client.Channels.Get("best");
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
                var encryptedChannel = _client.Channels.Get("encrypted", new ChannelOptions(true));
                SetState(encryptedChannel, ChannelState.Attached);
                bool msgReceived = false, 
                    errorEmitted = false;
                encryptedChannel.Subscribe(msg =>
                {
                    msgReceived = true;
                });
                encryptedChannel.Error += (sender, args) =>
                {
                    errorEmitted = true;
                    Done();
                };

                var message = new Message("name", "encrypted with otherChannelOptions");
                new MessageHandler(Protocol.Json).EncodePayloads(otherChannelOptions, new[] {message});
                await _client.FakeMessageReceived(message, encryptedChannel.Name);

                WaitOne();

                msgReceived.Should().BeTrue();
                errorEmitted.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTL7e")]
            public async Task WithMessageThatCantBeDecoded_ShouldDeliverMessageWithResidualEncodingAndEmitTheErrorOnTheChannel()
            {
                var channel = _client.Channels.Get("test");
                SetState(channel, ChannelState.Attached);

                Message receivedMessage = null;
                channel.Subscribe(msg =>
                {
                    receivedMessage = msg;
                });

                ErrorInfo error = null;
                channel.Error += (sender, args) =>
                {
                    error = args.Reason;
                    Done();
                };

                var message = new Message("name", "encrypted with otherChannelOptions") { Encoding = "json" };
                await _client.FakeMessageReceived(message, channel.Name);

                WaitOne();

                error.Should().NotBeNull();
                receivedMessage.Encoding.Should().Be("json");
            }

            [Fact]
            public async Task WithMultipleMessages_ShouldSetIdOnEachMessage()
            {
                var channel = _client.Channels.Get("test");

                SetState(channel, ChannelState.Attached);

                List<Message> receivedMessages = new List<Message>();
                channel.Subscribe(msg =>
                {
                    receivedMessages.Add(msg);
                });

                var protocolMessage = SetupTestProtocolmessage();
                await _client.FakeProtocolMessageReceived(protocolMessage);

                receivedMessages.Should().HaveCount(3);
                receivedMessages.Select(x => x.Id)
                    .Should()
                    .BeEquivalentTo(new[]
                        {$"{protocolMessage.Id}:0", $"{protocolMessage.Id}:1", $"{protocolMessage.Id}:2"});
            }

            [Fact]
            public async Task WithMultipleMessagesHaveIds_ShouldPreserveTheirIds()
            {
                var channel = _client.Channels.Get("test");

                SetState(channel, ChannelState.Attached);

                List<Message> receivedMessages = new List<Message>();
                channel.Subscribe(msg =>
                {
                    receivedMessages.Add(msg);
                });

                var protocolMessage = SetupTestProtocolmessage(messages: new[]
                {
                    new Message("message1", "data") {Id = "1"},
                    new Message("message2", "data") {Id = "2"},
                    new Message("message3", "data") {Id = "3"},
                });
                    
                await _client.FakeProtocolMessageReceived(protocolMessage);

                receivedMessages.Should().HaveCount(3);
                receivedMessages.Select(x => x.Id)
                    .Should()
                    .BeEquivalentTo("1", "2", "3");
            }

            [Theory]
            [InlineData("protocolMessageConnId", null, "protocolMessageConnId")]
            [InlineData("protocolMessageConnId", "messageConId", "messageConId")]
            public async Task WithMessageWithConnectionIdEqualTo_ShouldBeEqualToExpected(string protocolMessageConId, string messageConId, string expectedConnId)
            {
                var channel = _client.Channels.Get("test");

                SetState(channel, ChannelState.Attached);

                Message receivedMessage = null;
                channel.Subscribe(msg =>
                {
                    receivedMessage = msg;
                });

                var protocolMessage = SetupTestProtocolmessage(connectionId: protocolMessageConId, messages: new[]
                {
                    new Message("message1", "data") {Id = "1", ConnectionId = messageConId},
                });

                await _client.FakeProtocolMessageReceived(protocolMessage);

                receivedMessage.ConnectionId.Should().Be(expectedConnId);
            }

            [Fact]
            public async Task WithProtocolMessage_ShouldSetMessageTimestampWhenNotThere()
            {
                var channel = _client.Channels.Get("test");

                SetState(channel, ChannelState.Attached);

                List<Message> receivedMessages = new List<Message>();
                channel.Subscribe(msg =>
                {
                    receivedMessages.Add(msg);
                });

                var protocolMessage = SetupTestProtocolmessage(timestamp: Now, messages: new[]
                {
                    new Message("message1", "data"),
                    new Message("message1", "data") {Timestamp = Now.AddMinutes(1)},
                });

                await _client.FakeProtocolMessageReceived(protocolMessage);

                receivedMessages.First().Timestamp.Should().Be(Now);
                receivedMessages.Last().Timestamp.Should().Be(Now.AddMinutes(1));
            }

            private ProtocolMessage SetupTestProtocolmessage(string connectionId = null, DateTimeOffset? timestamp = null,  Message[] messages = null)
            {
                var protocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Message, "test")
                {
                    ConnectionId = connectionId,
                    Timestamp = timestamp,
                    Id = "protocolMessageId",
                    Messages = messages ?? new[]
                    {
                        new Message("message1", "data"),
                        new Message("message2", "data"),
                        new Message("message3", "data"),
                    }
                };
                return protocolMessage;
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
            public async Task ShouldRemoveHandlerFromSubscribers()
            {
                _channel.Subscribe(_handler);
                _channel.Unsubscribe(_handler);
                await _client.FakeMessageReceived(new Message("test", "best"), _channel.Name);

                //Handler should not throw
            }

            [Fact]
            [Trait("spec", "RTL8b")]
            public async Task WithEventName_ShouldUnsubscribeHandlerFromTheSpecifiedEvent()
            {
                _channel.Subscribe("test", _handler);
                _channel.Unsubscribe("test", _handler);
                await _client.FakeMessageReceived(new Message("test", "best"), _channel.Name);
                //Handler should not throw
            }

            public UnsubscribeSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); //Easier to test encoding with the json protocol
                _channel = _client.Channels.Get("test");

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
            [Trait("spec", "RTL10a")]
            public async Task ShouldCallRestClientToGetHistory()
            {
                var channel = _client.Channels.Get("history");

                await channel.HistoryAsync();

                Assert.Equal($"/channels/{channel.Name}/messages", LastRequest.Url);
            }

            [Fact]
            [Trait("spec", "RTL10b")]
            public async Task WithUntilAttach_ShouldPassAttachedSerialToHistoryQuery()
            {
                var channel = _client.Channels.Get("history");
                SetState(channel, ChannelState.Attached, message: new ProtocolMessage(ProtocolMessage.MessageAction.Attached) { ChannelSerial = "101"});

                await channel.HistoryAsync(untilAttach: true);

                LastRequest.QueryParameters.Should()
                    .ContainKey("fromSerial")
                    .WhichValue.Should().Be("101");
            }

            [Fact]
            public async Task WithUntilAttachButChannelNotAttached_ShouldThrowException()
            {
                var channel = _client.Channels.Get("history");

                var ex = await Assert.ThrowsAsync<AblyException>(() => channel.HistoryAsync(true));
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