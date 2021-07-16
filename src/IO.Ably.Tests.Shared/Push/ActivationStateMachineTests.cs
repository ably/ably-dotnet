using System.Runtime.CompilerServices;
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
    }
}
