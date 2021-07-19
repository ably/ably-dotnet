using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Push;
using IO.Ably.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public class ActivationStateMachineTests
    {
        public class GeneralTests : MockHttpRestSpecs
        {
            [Fact]
            public async Task LoadPersistedLocalDevice_ShouldLoadAllSavedProperties()
            {
                var mobileDevice = new FakeMobileDevice();
                void SetSetting(string key, string value) => mobileDevice.SetPreference(key, value, PersistKeys.Device.SharedName);

                var stateMachine = new ActivationStateMachine(GetRestClient(), mobileDevice);

                var deviceId = "deviceId";
                SetSetting(PersistKeys.Device.DEVICE_ID, deviceId);
                var clientid = "clientId";
                SetSetting(PersistKeys.Device.CLIENT_ID, clientid);
                var deviceSecret = "secret";
                SetSetting(PersistKeys.Device.DEVICE_SECRET, deviceSecret);
                var identityToken = "token";
                SetSetting(PersistKeys.Device.DEVICE_TOKEN, identityToken);
                var tokenType = "fcm";
                SetSetting(PersistKeys.Device.TOKEN_TYPE, tokenType);
                var token = "registration_token";
                SetSetting(PersistKeys.Device.TOKEN, token);

                var localDevice = stateMachine.LoadPersistedLocalDevice();
                localDevice.Platform.Should().Be(mobileDevice.DevicePlatform);
                localDevice.FormFactor.Should().Be(mobileDevice.FormFactor);

                localDevice.Id.Should().Be(deviceId);
                localDevice.ClientId.Should().Be(clientid);
                localDevice.DeviceSecret.Should().Be(deviceSecret);
                localDevice.DeviceIdentityToken.Should().Be(identityToken);
                localDevice.RegistrationToken.Type.Should().Be(tokenType);
                localDevice.RegistrationToken.Token.Should().Be(token);
            }

            public GeneralTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }


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

            [Fact]
            [Trait("spec", "RSH3b3")]
            public async Task ShouldBeAbleToHandleGotPushDeviceDetails()
            {
                var state = GetState();
                state.CanHandleEvent(new ActivationStateMachine.GotPushDeviceDetails()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3b3b")]
            [Trait("spec", "RSH3b3d")]
            public async Task GotPushDeviceDetails_ShouldSetStateToWaitingForDeviceRegistrationAndRegisterDeviceWithAblyRestApi()
            {
                var (state, machine) = GetStateAndStateMachine();
                var token = "token";

                machine.LocalDevice = LocalDevice.Create(mobileDevice: MobileDevice);
                machine.LocalDevice.Push.Recipient = new JObject();
                RestClient.ExecuteHttpRequest = request =>
                {
                    var localDevice = JObject.FromObject(new LocalDevice());
                    localDevice["deviceIdentityToken"] = JObject.FromObject(new { token });
                    var response = localDevice.ToString();

                    return Task.FromResult(new AblyResponse()
                        { StatusCode = HttpStatusCode.OK, TextResponse = response });
                };
                var (nextState, nextStateFunction) =
                    await state.Transition(new ActivationStateMachine.GotPushDeviceDetails());

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForDeviceRegistration>();
                (await nextStateFunction()).Should().BeOfType<ActivationStateMachine.GotDeviceRegistration>().Which
                    .DeviceIdentityToken.Should().Be(token);
            }

            [Fact]
            [Trait("spec", "RSH3b3c")]
            [Trait("spec", "RSH3b3d")]
            public async Task GotPushDeviceDetails_WhenRegisterDeviceFails_ShouldReturnGettingDeviceRegistrationFailedEventWithErrorDetailsInTheEvent()
            {
                var (state, machine) = GetStateAndStateMachine();

                machine.LocalDevice = LocalDevice.Create(mobileDevice: MobileDevice);
                machine.LocalDevice.Push.Recipient = new JObject();
                RestClient.ExecuteHttpRequest = request => throw new AblyException("Error", ErrorCodes.InternalError);

                var (nextState, nextStateFunction) =
                    await state.Transition(new ActivationStateMachine.GotPushDeviceDetails());

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForDeviceRegistration>();
                (await nextStateFunction()).Should().BeOfType<ActivationStateMachine.GettingDeviceRegistrationFailed>()
                    .Which
                    .Reason.Message.Should().Be("Error");
            }

            [Fact]
            [Trait("spec", "RSH3b4")]
            public async Task ShouldBeAbleToHandleGettingDeviceRegistrationFailed()
            {
                var state = GetState();
                state.CanHandleEvent(new ActivationStateMachine.GettingDeviceRegistrationFailed(new ErrorInfo())).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3b4")]
            [Trait("spec", "RSH3c3b")]
            public async Task GettingDeviceRegistrationFailed_ShouldTransitionToNotActivated()
            {
                var state = GetState();

                var (nextState, nextEventFunc) =
                    await state.Transition(new ActivationStateMachine.GettingDeviceRegistrationFailed(new ErrorInfo()));

                nextState.Should().BeOfType<ActivationStateMachine.NotActivated>();
                (await nextEventFunc()).Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RSH3b4")]
            [Trait("spec", "RSH3c3a")]
            public async Task GettingDeviceRegistrationFailed_ShouldTriggerActivatedCallbackAndPassErrorInfo()
            {
                var state = GetState();

                var errorInfo = new ErrorInfo("Reason");
                var awaiter = new TaskCompletionAwaiter();
                MobileDevice.Callbacks.ActivatedCallback = reason =>
                {
                    reason.Should().BeSameAs(errorInfo);
                    awaiter.SetCompleted();
                    return Task.CompletedTask;
                };

                await state.Transition(new ActivationStateMachine.GettingDeviceRegistrationFailed(errorInfo));
                (await awaiter.Task).Should().BeTrue();
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

        [Trait("spec", "RSH3c")]
        public class WaitingForDeviceRegistrationTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH3c1")]
            public async Task ShouldBeAbleToHandleCallActivateEvent()
            {
                GetState().CanHandleEvent(new ActivationStateMachine.CalledActivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3c1a")]
            public async Task WithCalledActivated_ShouldNotChangeState()
            {
                var state = GetState();

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                nextState.Should().BeSameAs(state);
                (await nextEventFunc()).Should().BeNull();
            }

            [Fact]
            [Trait("spec", "RSH3c2")]
            public async Task ShouldBeAbleToHandleGotDeviceRegistration()
            {
                GetState().CanHandleEvent(new ActivationStateMachine.GotDeviceRegistration(string.Empty)).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3c2a")]
            public async Task WithGotDeviceRegistration_ShouldUpdateDeviceDetails()
            {
                var (state, machine) = GetStateAndStateMachine();

                await state.Transition(new ActivationStateMachine.GotDeviceRegistration("token"));

                // (RSH3c2a)
                machine.LocalDevice.DeviceIdentityToken.Should().Be("token");
                // Check it was saved in device storage as well
                MobileDevice.GetPreference(PersistKeys.Device.DEVICE_TOKEN, PersistKeys.Device.SharedName).Should()
                    .Be("token");
            }

            [Fact]
            [Trait("spec", "RSH3c2b")]
            [Trait("spec", "RSH3c2c")]
            public async Task WithGotDeviceRegistration_ShouldTriggerCallbackAndUpdateStateToWaitingForNewPushDeviceDetails()
            {
                var state = GetState();

                var awaiter = new TaskCompletionAwaiter();
                MobileDevice.Callbacks.ActivatedCallback = reason =>
                {
                    reason.Should().BeNull();
                    awaiter.SetCompleted();
                    return Task.CompletedTask;
                };

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.GotDeviceRegistration("token"));

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForNewPushDeviceDetails>();

                (await nextEventFunc()).Should().BeNull();
                (await awaiter.Task).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3c3")]
            public async Task ShouldBeAbleHandleGettingDeviceRegistrationFailedEvent()
            {
                GetState().CanHandleEvent(new ActivationStateMachine.GettingDeviceRegistrationFailed(new ErrorInfo()))
                    .Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3c3a")]
            [Trait("spec", "RSH3c3b")]
            public async Task WithGettingDeviceRegistrationFailed_ShouldTriggerCallbackWithErrorAndTransitionToNotActivated()
            {
                var state = GetState();

                var error = new ErrorInfo("Reason");
                var awaiter = new TaskCompletionAwaiter();
                MobileDevice.Callbacks.ActivatedCallback = reason =>
                {
                    reason.Should().BeSameAs(error);
                    awaiter.SetCompleted();
                    return Task.CompletedTask;
                };

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.GettingDeviceRegistrationFailed(error));

                nextState.Should().BeOfType<ActivationStateMachine.NotActivated>();
                (await nextEventFunc()).Should().BeNull();
                (await awaiter.Task).Should().BeTrue();
            }

            public WaitingForDeviceRegistrationTests(ITestOutputHelper output)
                : base(output)
            {
                RestClient = GetRestClient();
                MobileDevice = new FakeMobileDevice();
            }

            private ActivationStateMachine.WaitingForDeviceRegistration GetState()
            {
                var stateMachine = new ActivationStateMachine(RestClient, MobileDevice, RestClient.Logger);
                return new ActivationStateMachine.WaitingForDeviceRegistration(stateMachine);
            }

            private (ActivationStateMachine.WaitingForDeviceRegistration, ActivationStateMachine) GetStateAndStateMachine(AblyRest restClient = null)
            {
                var stateMachine = new ActivationStateMachine(restClient ?? RestClient, MobileDevice, RestClient.Logger);
                return (new ActivationStateMachine.WaitingForDeviceRegistration(stateMachine), stateMachine);
            }

            public FakeMobileDevice MobileDevice { get; set; }

            public AblyRest RestClient { get; set; }
        }

        [Trait("spec", "RSH3d")]
        public class WaitingForNewPushDeviceDetailsTests : MockHttpRestSpecs
        {
            [Fact]
            [Trait("spec", "RSH3d1")]
            public async Task ShouldHandleCalledActivate()
            {
                GetState().CanHandleEvent(new ActivationStateMachine.CalledActivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3d1a")]
            [Trait("spec", "RSH3d1b")]
            public async Task WithCalledActivate_ShouldTriggerCallbackAndReturnTheSameState()
            {
                var state = GetState();

                var awaiter = new TaskCompletionAwaiter();
                MobileDevice.Callbacks.ActivatedCallback = error =>
                {
                    error.Should().BeNull();
                    awaiter.SetCompleted();
                    return Task.CompletedTask;
                };

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledActivate());

                nextState.Should().BeSameAs(state);
                (await nextEventFunc()).Should().BeNull();

                (await awaiter.Task).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3d2")]
            public async Task ShouldHandleCalledDeactivate()
            {
                GetState().CanHandleEvent(new ActivationStateMachine.CalledDeactivate()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3d2b")]
            [Trait("spec", "RSH3d2c")]
            public async Task WithCalledDeactivateEvent_ShouldTransitionToWaitingForDeregistrationAndCallRemoveDeviceRestApi()
            {
                var (state, machine) = GetStateAndStateMachine();

                machine.LocalDevice = LocalDevice.Create();

                var awaiter = new TaskCompletionAwaiter();
                RestClient.ExecuteHttpRequest = request =>
                {
                    request.Method.Should().Be(HttpMethod.Delete);

                    awaiter.SetCompleted();
                    return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK, TextResponse = string.Empty });
                };

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledDeactivate());

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForDeregistration>().Which.PreviousState.Should().BeSameAs(state);

                (await nextEventFunc()).Should().BeOfType<ActivationStateMachine.Deregistered>();
                (await awaiter.Task).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3d2b")]
            [Trait("spec", "RSH3d2c")]
            public async Task WithCalledDeactivateEvent_WhenCallToRemoveDeviceFails_ShouldTransitionToWaitingAndTheNextEventShouldBeDeregistrationFailed()
            {
                var (state, machine) = GetStateAndStateMachine();

                machine.LocalDevice = LocalDevice.Create();

                var error = new ErrorInfo();
                RestClient.ExecuteHttpRequest = request => throw new AblyException(error);

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.CalledDeactivate());

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForDeregistration>().Which.PreviousState.Should().BeSameAs(state);

                (await nextEventFunc()).Should().BeOfType<ActivationStateMachine.DeregistrationFailed>().Which.Reason.Should().BeSameAs(error);
            }

            [Fact]
            [Trait("spec", "RSH3d3")]
            public async Task ShouldHandleGotPushDeviceDetails()
            {
                GetState().CanHandleEvent(new ActivationStateMachine.GotPushDeviceDetails()).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3d3b")]
            public async Task WithGotPushDeviceDetailsEvent_ShouldPatchDeviceDetails()
            {
                var (state, machine) = GetStateAndStateMachine();

                machine.LocalDevice = LocalDevice.Create();
                machine.LocalDevice.DeviceIdentityToken = "token";
                machine.LocalDevice.Push.Recipient = new JObject();

                var awaiter = new TaskCompletionAwaiter();
                RestClient.ExecuteHttpRequest = request =>
                {
                    request.Url.Should().Be($"/push/deviceRegistrations/{machine.LocalDevice.Id}");
                    request.Method.Should().Be(new HttpMethod("PATCH"));

                    awaiter.SetCompleted();
                    return Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK, TextResponse = string.Empty });
                };

                var (_, nextEventFunc) = await state.Transition(new ActivationStateMachine.GotPushDeviceDetails());

                await nextEventFunc();
                (await awaiter.Task).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RSH3d3c")]
            [Trait("spec", "RSH3d3d")]
            public async Task WithGotPushDeviceDetailsEvent_WhenPatchDeviceDetailsSucceeds_ShouldTransitionToWaitingForRegistrationSyncAndProduceRegistrationSyncedEvent()
            {
                var (state, machine) = GetStateAndStateMachine();

                machine.LocalDevice = LocalDevice.Create();
                machine.LocalDevice.DeviceIdentityToken = "token";
                machine.LocalDevice.Push.Recipient = new JObject();

                RestClient.ExecuteHttpRequest = request => Task.FromResult(new AblyResponse() { StatusCode = HttpStatusCode.OK, TextResponse = string.Empty });

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.GotPushDeviceDetails());

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForRegistrationSync>().Which.FromEvent.Should().BeOfType<ActivationStateMachine.GotPushDeviceDetails>();
                (await nextEventFunc()).Should().BeOfType<ActivationStateMachine.RegistrationSynced>();
            }

            [Fact]
            [Trait("spec", "RSH3d3c")]
            [Trait("spec", "RSH3d3d")]
            public async Task WithGotPushDeviceDetailsEvent_WhenPatchDeviceDetailsFails_ShouldTransitionToWaitingForRegistrationSyncAndProduceSyncRegistrationFailedEvent()
            {
                var (state, machine) = GetStateAndStateMachine();

                machine.LocalDevice = LocalDevice.Create();
                machine.LocalDevice.DeviceIdentityToken = "token";
                machine.LocalDevice.Push.Recipient = new JObject();

                var error = new ErrorInfo();
                RestClient.ExecuteHttpRequest = request => throw new AblyException(error);

                var (nextState, nextEventFunc) = await state.Transition(new ActivationStateMachine.GotPushDeviceDetails());

                nextState.Should().BeOfType<ActivationStateMachine.WaitingForRegistrationSync>().Which.FromEvent.Should().BeOfType<ActivationStateMachine.GotPushDeviceDetails>();
                (await nextEventFunc()).Should().BeOfType<ActivationStateMachine.SyncRegistrationFailed>().Which.Reason.Should().BeSameAs(error);
            }

            public WaitingForNewPushDeviceDetailsTests(ITestOutputHelper output)
                : base(output)
            {
                RestClient = GetRestClient();
                MobileDevice = new FakeMobileDevice();
            }

            private ActivationStateMachine.WaitingForNewPushDeviceDetails GetState()
            {
                var stateMachine = new ActivationStateMachine(RestClient, MobileDevice, RestClient.Logger);
                return new ActivationStateMachine.WaitingForNewPushDeviceDetails(stateMachine);
            }

            private (ActivationStateMachine.WaitingForNewPushDeviceDetails, ActivationStateMachine) GetStateAndStateMachine(AblyRest restClient = null)
            {
                var stateMachine = new ActivationStateMachine(restClient ?? RestClient, MobileDevice, RestClient.Logger);
                return (new ActivationStateMachine.WaitingForNewPushDeviceDetails(stateMachine), stateMachine);
            }

            public FakeMobileDevice MobileDevice { get; set; }

            public AblyRest RestClient { get; set; }
        }
    }
}
