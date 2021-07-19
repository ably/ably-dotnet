﻿using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using IO.Ably.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public class ActivationStateMachineTests
    {
        public class NotActivatedStateTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH3a1")]
            public void CalledDeactivateEvent_CanBeHandled()
            {
                var state = GetState();

                state.CanHandleEvent(new ActivationStateMachine.CalledDeactivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3a1a")]
            public async Task CalledDeactivateEvent_ShouldTriggerDeactivatedCallback()
            {
                var state = GetState();
                var awaiter = new TaskCompletionAwaiter();
                bool callbackExecuted = false;
                MobileDevice.Callbacks.DeactivatedCallback = error =>
                {
                    callbackExecuted = true;
                    awaiter.SetCompleted();
                    return Task.CompletedTask;
                };

                await state.Transition(new ActivationStateMachine.CalledDeactivate());

                (await awaiter.Task).Should().BeTrue();
                callbackExecuted.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3a1b")]
            public async Task CalledDeactivateEvent_ShouldKeepTheSameState()
            {
                var state = GetState();

                var (nextState, eventFunc) = await state.Transition(new ActivationStateMachine.CalledDeactivate());

                nextState.Should().BeSameAs(state);
                (await eventFunc()).Should().BeNull(); // No more events should be generated.
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            public void CalledActivateEvent_CanBeHandled()
            {
                var state = GetState();

                state.CanHandleEvent(new ActivationStateMachine.CalledActivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            [Trait("spec", "RSH3a2a4")]
            public async Task
                WithCalledActivate_WhenLocalDeviceHasDeviceIdentityToken_ShouldCheckClientIdCompatibility()
            {
                var restClient = GetRestClient(null, options => options.ClientId = "999");
                var (state, stateMachine) = GetStateAndStateMachine(restClient);

                stateMachine.LocalDevice = new LocalDevice() { DeviceIdentityToken = "token", ClientId = "123" };

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());
                nextState.Should().BeOfType<ActivationStateMachine.WaitingForRegistrationSync>();

                var nextEvent = await nextEventFunc();
                nextEvent.Should().BeOfType<ActivationStateMachine.SyncRegistrationFailed>()
                    .Which.Reason.Code.Should().Be(ErrorCodes.ActivationFailedClientIdMismatch);
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            [Trait("spec", "RSH3a2a3")]
            public async Task WithCalledActivate_WhenLocalDeviceHasDeviceIdentityToken_AndSuccessfulDeviceRegistration_ShouldReturnRegistrationSyncedNextEvent()
            {
                var restClient = GetRestClient(request =>
                {
                    if (request.Url.StartsWith("/push/deviceRegistrations"))
                    {
                        return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK, TextResponse = LocalDevice.Create().ToJson() });
                    }

                    return Task.FromResult(new AblyResponse());
                });

                var (state, stateMachine) = GetStateAndStateMachine(restClient);
                var localDevice = LocalDevice.Create();
                localDevice.DeviceIdentityToken = "token";
                stateMachine.LocalDevice = localDevice;

                var (_, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                var nextEvent = await nextEventFunc();
                nextEvent.Should().BeOfType<ActivationStateMachine.RegistrationSynced>();
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            [Trait("spec", "RSH3a2a3")]
            public async Task WithCalledActivate_WhenLocalDeviceHasDeviceIdentityToken_AndFailedDeviceRegistration_ShouldReturnSyncRegistrationFailedNextEvent()
            {
                var restClient = GetRestClient(request =>
                {
                    if (request.Url.StartsWith("/push/deviceRegistrations"))
                    {
                        throw new AblyException("Invalid request");
                    }

                    return Task.FromResult(new AblyResponse());
                });

                var (state, stateMachine) = GetStateAndStateMachine(restClient);

                var localDevice = LocalDevice.Create();
                localDevice.DeviceIdentityToken = "token";
                stateMachine.LocalDevice = localDevice;

                var (_, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                var nextEvent = await nextEventFunc();
                nextEvent.Should().BeOfType<ActivationStateMachine.SyncRegistrationFailed>();
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            [Trait("spec", "RSH3a2b")]
            public async Task WithCalledActivate_WithoutCreatedLocalDevice_ShouldCreateNewLocalDeviceAndPersistIt()
            {
                var (state, stateMachine) = GetStateAndStateMachine(GetRestClient(null, options => options.ClientId = "123"));

                stateMachine.LocalDevice = new LocalDevice();

                await state.Transition(new ActivationStateMachine.CalledActivate());

                MobileDevice.GetPreference(PersistKeys.Device.DEVICE_ID, PersistKeys.Device.SharedName)
                    .Should().NotBeEmpty();

                MobileDevice.GetPreference(PersistKeys.Device.CLIENT_ID, PersistKeys.Device.SharedName)
                    .Should().NotBeEmpty();

                MobileDevice.GetPreference(PersistKeys.Device.DEVICE_SECRET, PersistKeys.Device.SharedName)
                    .Should().NotBeEmpty();
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            [Trait("spec", "RSH3a2d")]
            public async Task WithCalledActivate_WithoutCreatedLocalDevice_ShouldTriggerGetRegistrationToken()
            {
                var (state, stateMachine) = GetStateAndStateMachine(GetRestClient(null, options => options.ClientId = "123"));

                stateMachine.LocalDevice = new LocalDevice();

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                (await nextEventFunc()).Should().BeNull(); // No next next
                nextState.Should().BeOfType<ActivationStateMachine.WaitingForPushDeviceDetails>();
                MobileDevice.RequestRegistrationTokenCalled.Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3a2")]
            [Trait("spec", "RSH3a2c")]
            public async Task WithCalledActivate_WhenLocalDeviceHasPushDetails_ShouldTriggerGotPushDeviceDetailsEvent()
            {
                var (state, stateMachine) = GetStateAndStateMachine();

                var localDevice = LocalDevice.Create("123");
                localDevice.RegistrationToken = new RegistrationToken("test", "token");
                stateMachine.LocalDevice = localDevice;

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                (await nextEventFunc()).Should().BeOfType<ActivationStateMachine.GotPushDeviceDetails>();
                nextState.Should().BeOfType<ActivationStateMachine.WaitingForPushDeviceDetails>();
            }

            [Fact]
            [Trait("spec", "RSH3a3")]
            [Trait("spec", "RSH3a3a")]
            public async Task WithGotPushDeviceDetails_ShouldReturnTheSameStateWithoutAnyFurtherEvents()
            {
                var (state, stateMachine) = GetStateAndStateMachine();

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.GotPushDeviceDetails());

                (await nextEventFunc()).Should().BeNull();
                nextState.Should().BeSameAs(state);
            }

            [Fact]
            [Trait("spec", "RSH3a3")]
            public async Task NotActivated_ShouldBeAbleToHandleGotPushDeviceDetails()
            {
                var state = GetState();
                state.CanHandleEvent(new ActivationStateMachine.GotPushDeviceDetails()).Should().BeTrue();
            }

            [Fact]
            public async Task NotActivated_ShouldThrowIfItCannotHandleEventType()
            {
                var state = GetState();
                Func<Task> transitionWithWrongEvent = () => state.Transition(new ActivationStateMachine.RegistrationSynced());

                await transitionWithWrongEvent.Should().ThrowAsync<AblyException>();
            }

            private ActivationStateMachine.NotActivated GetState()
            {
                var stateMachine = new ActivationStateMachine(RestClient, MobileDevice, RestClient.Logger);
                return new ActivationStateMachine.NotActivated(stateMachine);
            }

            private (ActivationStateMachine.NotActivated, ActivationStateMachine) GetStateAndStateMachine(AblyRest restClient = null)
            {
                var stateMachine = new ActivationStateMachine(restClient ?? RestClient, MobileDevice, RestClient.Logger);
                return (new ActivationStateMachine.NotActivated(stateMachine), stateMachine);
            }

            public NotActivatedStateTests(ITestOutputHelper output)
                : base(output)
            {
                RestClient = GetRestClient();
                MobileDevice = new FakeMobileDevice();
            }

            public AblyRest RestClient { get; }

            public FakeMobileDevice MobileDevice { get; }
        }

        [Trait("spec", "RSH3b")]
        public class WaitingForPushDeviceDetailsTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH3b1")]
            public async Task ShouldBeAbleToHandleCalledActivate()
            {
                var state = GetState();
                state.CanHandleEvent(new ActivationStateMachine.CalledActivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3b1")]
            public async Task WithCalledActivateEvent_ShouldReturnSameState()
            {
                var state = GetState();
                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                nextState.Should().BeSameAs(state);
                (await nextEventFunc()).Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RSH3b2")]
            public async Task ShouldBeAbleToHandleCalledDeactivate()
            {
                var state = GetState();
                state.CanHandleEvent(new ActivationStateMachine.CalledDeactivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3b2a")]
            public async Task CalledDeactivateEvent_ShouldTriggerDeactivatedCallback()
            {
                var state = GetState();
                var awaiter = new TaskCompletionAwaiter();
                MobileDevice.Callbacks.DeactivatedCallback = error =>
                {
                    error.Should().BeNull();
                    awaiter.SetCompleted();
                    return Task.CompletedTask;
                };

                await state.Transition(new ActivationStateMachine.CalledDeactivate());
                (await awaiter.Task).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3b2b")]
            public async Task CalledDeactivateEvent_ShouldTransitionToNotActivatedWithNoFurtherEvents()
            {
                var state = GetState();
                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledDeactivate());

                nextState.Should().BeOfType<ActivationStateMachine.NotActivated>();
                (await nextEventFunc()).Should().BeNull();
            }

            public WaitingForPushDeviceDetailsTests(ITestOutputHelper output)
                : base(output)
            {
                RestClient = GetRestClient();
                MobileDevice = new FakeMobileDevice();
            }

            private ActivationStateMachine.WaitingForPushDeviceDetails GetState()
            {
                var stateMachine = new ActivationStateMachine(RestClient, MobileDevice, RestClient.Logger);
                return new ActivationStateMachine.WaitingForPushDeviceDetails(stateMachine);
            }

            private (ActivationStateMachine.WaitingForPushDeviceDetails, ActivationStateMachine) GetStateAndStateMachine(AblyRest restClient = null)
            {
                var stateMachine = new ActivationStateMachine(restClient ?? RestClient, MobileDevice, RestClient.Logger);
                return (new ActivationStateMachine.WaitingForPushDeviceDetails(stateMachine), stateMachine);
            }

            public FakeMobileDevice MobileDevice { get; set; }

            public AblyRest RestClient { get; set; }
        }
    }
}