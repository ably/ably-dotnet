using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Transport;
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
            public void WhenDetachedOrFailed_AllQueuedPresenceMessagesShouldBeDeletedAndFailCallbackInvoked(ChannelState state)
            {
                var client = GetConnectedClient();
                var channel = client.Channels.Get("test") as RealtimeChannel;
                var expectedError = new ErrorInfo();

                channel.Attach();

                bool didSucceed = false;
                ErrorInfo err = null;
                channel.Presence.Enter(null,
                    (b, info) =>
                        {
                            didSucceed = b;
                            err = info;
                        });

                channel.Presence.PendingPresenceQueue.Should().HaveCount(1);

                channel.SetChannelState(state, expectedError);
                channel.Presence.PendingPresenceQueue.Should().HaveCount(0);

                didSucceed.Should().BeFalse();
                err.Should().BeSameAs(expectedError);
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

            public GeneralSpecs(ITestOutputHelper output)
                : base(output)
            {
            }
        }

        [Trait("spec", "RTL2")]
        public class EventEmitterSpecs : ChannelSpecs
        {
            private IRealtimeChannel _channel;

            [Fact]
            [Trait("spec", "RTL2")]
            public void ValidateChannelEventAndChannelStateValues()
            {
                // ChannelState and ChannelEvent should have values that are aligned
                // to allow for casting between the two (with the exception of the Update event)
                // The assigned values are arbitary, but should be unique per event/state pair
                ((int)ChannelEvent.Initialized).Should().Be(0);
                ((int)ChannelEvent.Attaching).Should().Be(1);
                ((int)ChannelEvent.Attached).Should().Be(2);
                ((int)ChannelEvent.Detaching).Should().Be(3);
                ((int)ChannelEvent.Detached).Should().Be(4);
                ((int)ChannelEvent.Suspended).Should().Be(5);
                ((int)ChannelEvent.Failed).Should().Be(6);
                ((int)ChannelEvent.Update).Should().Be(7);

                // each ChannelState should have an equivelant ChannelEvent
                ((int)ChannelState.Failed).Should().Be((int)ChannelEvent.Failed);
                ((int)ChannelState.Detached).Should().Be((int)ChannelEvent.Detached);
                ((int)ChannelState.Detaching).Should().Be((int)ChannelEvent.Detaching);
                ((int)ChannelState.Initialized).Should().Be((int)ChannelEvent.Initialized);
                ((int)ChannelState.Attached).Should().Be((int)ChannelEvent.Attached);
                ((int)ChannelState.Attaching).Should().Be((int)ChannelEvent.Attaching);
                ((int)ChannelState.Suspended).Should().Be((int)ChannelEvent.Suspended);
            }

            [Theory]
            [InlineData(ChannelEvent.Attaching)]
            [InlineData(ChannelEvent.Attached)]
            [InlineData(ChannelEvent.Detaching)]
            [InlineData(ChannelEvent.Detached)]
            [InlineData(ChannelEvent.Failed)]
            [InlineData(ChannelEvent.Suspended)]
            [Trait("spec", "RTL2a")]
            [Trait("spec", "RTL2b")]
            [Trait("spec", "RTL2d")]
            [Trait("spec", "TH1")]
            [Trait("spec", "TH2")]
            [Trait("spec", "TH5")]
            public void ShouldEmitTheFollowingStates(ChannelEvent channelEvent)
            {
                var chanName = "test".AddRandomSuffix();
                var client = GetConnectedClient();
                var channel = client.Channels.Get(chanName);

                ChannelEvent sourceEvent = ChannelEvent.Update;
                ChannelState previousState = ChannelState.Failed;
                ChannelState newState = ChannelState.Initialized;
                channel.On(channelStateChange =>
                {
                    // RTL2d first argument should be an instance of ChannelStateChange
                    channelStateChange.Should().NotBeNull();
                    channelStateChange.Error.Should().BeNull();

                    // should be the state corresponding to the
                    // passed in ChannelEvent
                    // (which should be anything but Initialized)
                    newState = channelStateChange.Current;

                    // should always be Initialized
                    previousState = channelStateChange.Previous;

                    // TH5
                    sourceEvent = channelStateChange.Event;
                    Done();
                });

                (channel as RealtimeChannel).SetChannelState((ChannelState)channelEvent);

                WaitOne();

                channel.State.Should().Be((ChannelState)channelEvent);
                newState.Should().Be((ChannelState)channelEvent);
                previousState.Should().Be(ChannelState.Initialized);
                sourceEvent.Should().Be(channelEvent);
            }

            [Theory]
            [InlineData(ProtocolMessage.MessageAction.Attached)]
            [InlineData(ProtocolMessage.MessageAction.Detached)]
            [Trait("spec", "RTL2d")]
            public void ShouldSetTheErrorReasonIfPresent(ProtocolMessage.MessageAction action)
            {
                var chanName = "test".AddRandomSuffix();
                var client = GetConnectedClient();
                var channel = client.Channels.Get(chanName);

                ChannelState previousState = ChannelState.Failed;
                ChannelState newState = ChannelState.Initialized;
                channel.On(channelStateChange =>
                {
                    // RTL2d first argument should be an instance of ChannelStateChange
                    channelStateChange.Should().NotBeNull();
                    channelStateChange.Error.Should().NotBeNull();
                    channelStateChange.Error.Message.Should().Be(action.ToString());
                    channelStateChange.Error.Code.Should().Be((int)action);

                    // should be the state corresponding to the
                    // passed in ChannelEvent which should be
                    // anything but Initialized
                    newState = channelStateChange.Current;

                    // should always be Initialized
                    previousState = channelStateChange.Previous;
                    Done();
                });

                // any state change triggered by a ProtocolMessage that contains an error member
                // should populate the reason with that error in the corresponding state change event
                client.FakeProtocolMessageReceived(new ProtocolMessage(action, chanName)
                {
                    Error = new ErrorInfo(action.ToString(), (int)action)
                });

                WaitOne();

                channel.State.ToString().Should().Be(action.ToString());
                newState.ToString().Should().Be(action.ToString());
                previousState.Should().Be(ChannelState.Initialized);
            }

            [Fact]
            [Trait("spec", "RTL2c")]
            public void ShouldEmmitErrorWithTheErrorThatHasOccuredOnTheChannel()
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

            [Fact]
            [Trait("spec", "RTL2f")]
            [Trait("spec", "RTL2g")]
            [Trait("spec", "RTL12")]
            [Trait("spec", "TH2")]
            [Trait("spec", "TH3")]
            [Trait("spec", "TH4")]
            public async Task WhenAttachedProtocolMessageWithResumedFlagReceived_EmittedChannelStateChangeShouldIndicateResumed()
            {
                var client = GetConnectedClient();
                var channel = client.Channels.Get("test");
                var tsc = new TaskCompletionAwaiter(2000);

                channel.Once(ChannelEvent.Attached, stateChange =>
                {
                    // RTL2f
                    stateChange.Current.Should().Be(ChannelState.Attached);
                    stateChange.Previous.Should().Be(ChannelState.Initialized);
                    stateChange.Resumed.Should().BeFalse();
                    stateChange.Error.Should().BeNull();
                    tsc.SetCompleted();
                });

                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached)
                {
                    Channel = "test"
                });

                var result = await tsc.Task;
                result.Should().BeTrue("State change event should have been handled");
                channel.State.Should().Be(ChannelState.Attached);

                // reset
                tsc = new TaskCompletionAwaiter(2000);

                // RTL2g / RTL12
                channel.Once(ChannelEvent.Update, stateChange =>
                {
                    // RTL2f, TH2, TH4
                    stateChange.Current.Should().Be(ChannelState.Attached);
                    stateChange.Previous.Should().Be(ChannelState.Attached);
                    stateChange.Resumed.Should().BeFalse();
                    tsc.SetCompleted();
                });

                // Send another Attached message without the resume flag.
                // This should cause and Update event to be emitted.
                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached, "test"));

                result = await tsc.Task;
                result.Should().BeTrue("Update event should have been handled");
                channel.State.Should().Be(ChannelState.Attached);

                channel.Once(stateChange => throw new Exception("This should not be reached because resumed flag was set"));

                // send another Attached protocol message with the resumed flag set.
                // This should not trigger any channel event (per RTL12).
                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached)
                {
                    Channel = "test",
                    Flags = 4 // resumed
                });

                await Task.Delay(2000);
                tsc = new TaskCompletionAwaiter(500);

                // set detached so that the nexted Attached message with resume will pass
                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Detached, "test"));

                channel.Once(ChannelEvent.Attached, stateChange =>
                {
                    // RTL2f, TH4
                    stateChange.Resumed.Should().BeTrue();

                    // TH3
                    stateChange.Error.Message.Should().Be("test error");
                    tsc.SetCompleted();
                });

                await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Attached, "test")
                {
                    Flags = (int)ProtocolMessage.Flag.Resumed, // resumed
                    Error = new ErrorInfo("test error")
                });

                result = await tsc.Task;
                result.Should().BeTrue();
            }

            [Theory]
            [InlineData(ChannelState.Initialized)]
            [InlineData(ChannelState.Attaching)]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Detaching)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Failed)]
            [InlineData(ChannelState.Suspended)]
            [Trait("spec", "RTL2g")]
            public void ShouldNeverEmitAChannelEventForAStateEqualToThePreviousState(ChannelState state)
            {
                var client = GetConnectedClient();
                var channel = client.Channels.Get("test") as RealtimeChannel;
                bool stateDidChange = false;

                // set initial state
                channel.SetChannelState((ChannelState)state);

                channel.Once(stateChange =>
                {
                    stateDidChange = true;
                    throw new Exception("state change event should not be emitted");
                });

                // attempt to set the same state again
                channel.SetChannelState((ChannelState)state);

                stateDidChange.Should().BeFalse();
            }

            public EventEmitterSpecs(ITestOutputHelper output)
                : base(output)
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

            [Fact]
            [Trait("spec", "RTL2d")]
            [Trait("spec", "RTL3d")]
            public async Task WhenChannelIsSuspended_WhenConnectionBecomesConnectedAttemptAttach()
            {
                var client = GetConnectedClient();
                var channel = client.Channels.Get("test".AddRandomSuffix());
                await client.WaitForState(ConnectionState.Connected);
                await client.ConnectionManager.SetState(new ConnectionSuspendedState(client.ConnectionManager, Logger));
                await client.WaitForState(ConnectionState.Suspended);

                (channel as RealtimeChannel).SetChannelState(ChannelState.Suspended);

                await client.ConnectionManager.SetState(new ConnectionConnectedState(client.ConnectionManager, new ConnectionInfo("1", 100, "connectionKey", string.Empty)));

                await client.WaitForState(ConnectionState.Connected);
                client.Connection.State.Should().Be(ConnectionState.Connected);
                channel.State.Should().Be(ChannelState.Attaching);

                var tsc = new TaskCompletionAwaiter(15000);
                channel.Once(ChannelEvent.Suspended, s =>
                {
                    /* RTL2d */
                    s.Error.Should().NotBeNull();
                    s.Error.Message.Should().StartWith("Channel didn't attach within");
                    s.Error.Code.Should().Be(90007);
                    tsc.SetCompleted();
                });

                var completed = await tsc.Task;
                completed.Should().BeTrue("channel should have become Suspended again");
            }

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3c")]
            public async Task WhenConnectionIsSuspended_AttachingOrAttachedChannelsShouldTrasitionToSuspended(ChannelState state)
            {
                (_channel as RealtimeChannel).SetChannelState(state);

                _client.Close();

                await _client.ConnectionManager.SetState(new ConnectionSuspendedState(_client.ConnectionManager, Logger));

                _client.Connection.State.Should().Be(ConnectionState.Suspended);
                _channel.State.Should().Be(ChannelState.Suspended);
            }

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [InlineData(ChannelState.Failed)]
            [InlineData(ChannelState.Suspended)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Detaching)]
            [InlineData(ChannelState.Initialized)]
            [Trait("spec", "RTL3e")]
            public async Task WhenConnectionIsDisconnected_ChannelStateShouldNotChange(ChannelState state)
            {
                (_channel as RealtimeChannel).SetChannelState(state);

                _client.Close();

                await _client.ConnectionManager.SetState(new ConnectionDisconnectedState(_client.ConnectionManager, Logger));

                _client.Connection.State.Should().Be(ConnectionState.Disconnected);
                _channel.State.Should().Be(state);
            }

            public ConnectionStateChangeEffectSpecs(ITestOutputHelper output)
                : base(output)
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
                // Closed
                _client.Connection.ConnectionState = new ConnectionClosedState(_client.ConnectionManager, Logger);
                Assert.Throws<AblyException>(() => _client.Channels.Get("closed").Attach());

                // Closing
                _client.Connection.ConnectionState = new ConnectionClosingState(_client.ConnectionManager, Logger);
                Assert.Throws<AblyException>(() => _client.Channels.Get("closing").Attach());

                // Suspended
                _client.Connection.ConnectionState = new ConnectionSuspendedState(_client.ConnectionManager, Logger);
                var error = Assert.Throws<AblyException>(() => _client.Channels.Get("suspended").Attach());
                error.ErrorInfo.Code.Should().Be(500);

                // Failed
                _client.Connection.ConnectionState = new ConnectionFailedState(_client.ConnectionManager, ErrorInfo.ReasonFailed, Logger);
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
            [InlineData(ChannelState.Detaching)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Failed)]
            [Trait("spec", "RTL4f")]
            public async Task ShouldBecomeSuspendedIfAttachMessageNotReceivedWithinDefaultTimeout(ChannelState previousState)
            {
                SetState(_channel, previousState);

                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                _channel.Attach();

                await Task.Delay(120);
                for (int i = 0; i < 10; i++)
                {
                    if (_channel.State != previousState)
                    {
                        await Task.Delay(50);
                    }
                    else
                    {
                        break;
                    }
                }

                for (var i = 0; i < 5; i++)
                {
                    if (_channel.State == ChannelState.Attaching)
                    {
                        await Task.Delay(50);
                    }
                    else
                    {
                        break;
                    }
                }

                _channel.State.Should().Be(ChannelState.Suspended);
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
            public void WithACallback_ShouldCallCallbackWithErrorIfAttachFails()
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

            public ChannelAttachSpecs(ITestOutputHelper output)
                : base(output)
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

            [Retry]
            [Trait("spec", "RTL5f")]
            public async Task ShouldReturnToPreviousStateIfDetachedMessageWasNotReceivedWithinDefaultTimeout()
            {
                SetState(ChannelState.Attached);
                _client.Options.RealtimeRequestTimeout = TimeSpan.FromMilliseconds(100);
                bool detachSuccess = true;
                ErrorInfo detachError = null;

                // use a TaskCompletionSource to let us know when the Detach event has been handled
                TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>(false);
                _channel.Detach((success, error) =>
                {
                    detachSuccess = success;
                    detachError = error;
                    tsc.SetResult(true);
                });

                // timeout the tsc incase the detached event never happens
                async Task Timeoutfn()
                {
                    await Task.Delay(1000);
                    tsc.TrySetCanceled();
                }
#pragma warning disable 4014
                Timeoutfn();
#pragma warning restore 4014
                await tsc.Task;

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

            [Retry] // replaces fact
            [Trait("spec", "RTL5e")]
            public void WithACallback_ShouldCallCallbackWithErrorIfDetachFails()
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

            public ChannelDetachSpecs(ITestOutputHelper output)
                : base(output)
            {
                _client = GetConnectedClient();
                _channel = _client.Channels.Get("test");
            }
        }

        [Trait("spec", "RTL6")]
        public class PublishSpecs : ChannelSpecs
        {
            private AblyRealtime _client;

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

                    // Not connect the client
                    await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                    // Messages should be sent
                    LastCreatedTransport.LastMessageSend.Should().NotBeNull();
                    client.ConnectionManager.PendingMessages.Should().BeEmpty();
                }

                [Fact]
                [Trait("spec", "RTL6c2")]
                public async Task WhenConnectionIsDisconnecting_MessageShouldBeQueuedUntilConnectionMovesToConnected()
                {
                    var client = GetClientWithFakeTransport();
                    await client.ConnectionManager.SetState(new ConnectionDisconnectedState(client.ConnectionManager, Logger));
                    client.Connection.State.Should().Be(ConnectionState.Disconnected);
                    var channel = client.Channels.Get("connecting");
                    SetState(channel, ChannelState.Attached);
                    channel.Publish("test", "connecting");

                    LastCreatedTransport.LastMessageSend.Should().BeNull();
                    client.ConnectionManager.PendingMessages.Should().HaveCount(1);
                    client.ConnectionManager.PendingMessages.First().Message.Messages.First().Data.Should().Be("connecting");

                    // Now connect
                    client.Connect();
                    await client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

                    // Pending messages are sent
                    LastCreatedTransport.LastMessageSend.Should().NotBeNull();
                    client.ConnectionManager.PendingMessages.Should().BeEmpty();
                }

                public ConnectionStateConditions(ITestOutputHelper output)
                    : base(output)
                {
                    // Inherit _client from parent
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
                    channel.PublishAsync("test", "best", "123");

                    LastCreatedTransport.LastMessageSend.Messages.First().ClientId.Should().Be("123");
                }

                public ClientIdSpecs(ITestOutputHelper output)
                    : base(output)
                {
                }
            }

            public PublishSpecs(ITestOutputHelper output)
                : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); // Easier to test encoding with the json protocol
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
                    {
                        Done();
                    }
                });

                await _client.FakeMessageReceived(new Message("test", "best"), "Test");
                await _client.FakeMessageReceived(new Message(string.Empty, "best"), "Test");
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
                await _client.FakeMessageReceived(new Message(string.Empty, "best"), "test");
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
                    // do nothing
                });
                channel.State.Should().Be(ChannelState.Attaching);
            }

            [Retry(3)]
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
                new MessageHandler(Protocol.Json).EncodePayloads(otherChannelOptions, new[] { message });
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
                receivedMessages.Select(x => x.Id).Should().BeEquivalentTo(
                    new[] { $"{protocolMessage.Id}:0", $"{protocolMessage.Id}:1", $"{protocolMessage.Id}:2" });
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
                    new Message("message1", "data") { Id = "1" },
                    new Message("message2", "data") { Id = "2" },
                    new Message("message3", "data") { Id = "3" },
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

                var protocolMessage = SetupTestProtocolmessage(protocolMessageConId, messages: new[]
                {
                    new Message("message1", "data") { Id = "1", ConnectionId = messageConId },
                });

                await _client.FakeProtocolMessageReceived(protocolMessage);

                receivedMessage.ConnectionId.Should().Be(expectedConnId);
            }

            [Fact]
            public async Task WithProtocolMessage_ShouldSetMessageTimestampWhenNotThere()
            {
                SetNowFunc(() => DateTimeOffset.UtcNow);
                var timeStamp = Now;

                var channel = _client.Channels.Get("test");

                SetState(channel, ChannelState.Attached);

                List<Message> receivedMessages = new List<Message>();
                channel.Subscribe(msg =>
                {
                    receivedMessages.Add(msg);
                });

                var protocolMessage = SetupTestProtocolmessage(timestamp: timeStamp, messages: new[]
                {
                    new Message("message1", "data"),
                    new Message("message1", "data") { Timestamp = timeStamp.AddMinutes(1) },
                });

                await _client.FakeProtocolMessageReceived(protocolMessage);

                receivedMessages.First().Timestamp.Should().Be(timeStamp);
                receivedMessages.Last().Timestamp.Should().Be(timeStamp.AddMinutes(1));
            }

            private ProtocolMessage SetupTestProtocolmessage(string connectionId = null, DateTimeOffset? timestamp = null, Message[] messages = null)
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

            public SubscribeSpecs(ITestOutputHelper output)
                : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); // Easier to test encoding with the json protocol
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

                // Handler should not throw
            }

            [Fact]
            [Trait("spec", "RTL8b")]
            public async Task WithEventName_ShouldUnsubscribeHandlerFromTheSpecifiedEvent()
            {
                _channel.Subscribe("test", _handler);
                _channel.Unsubscribe("test", _handler);
                await _client.FakeMessageReceived(new Message("test", "best"), _channel.Name);

                // Handler should not throw
            }

            public UnsubscribeSpecs(ITestOutputHelper output)
                : base(output)
            {
                _client = GetConnectedClient(opts => opts.UseBinaryProtocol = false); // Easier to test encoding with the json protocol
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
                SetState(channel, ChannelState.Attached, message: new ProtocolMessage(ProtocolMessage.MessageAction.Attached) { ChannelSerial = "101" });

                await channel.HistoryAsync(true);

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

            public HistorySpecs(ITestOutputHelper output)
                : base(output)
            {
                _client = GetConnectedClient();
            }
        }

        protected void SetState(IRealtimeChannel channel, ChannelState state, ErrorInfo error = null, ProtocolMessage message = null)
        {
            (channel as RealtimeChannel).SetChannelState(state, error, message);
        }

        public ChannelSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
