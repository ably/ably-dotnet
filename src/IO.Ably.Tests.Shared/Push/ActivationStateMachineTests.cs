using FluentAssertions;
using IO.Ably.Push;
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
            public void NotActivateTest_ShouldBeAbleToHandleCalledDeactivate()
            {
                var state = GetState();

                state.CanHandleEvent(new ActivationStateMachine.CalledDeactivate()).Should().BeTrue();
            }

            private ActivationStateMachine.NotActivated GetState()
            {
                var restClient = GetRestClient();
                var mobileDevice = new FakeMobileDevice();
                var stateMachine = new ActivationStateMachine(restClient, mobileDevice, restClient.Logger);
                return new ActivationStateMachine.NotActivated(stateMachine);
            }

            public NotActivatedStateTests(ITestOutputHelper output)
                : base(output)
            {
            }
        }
    }
}
