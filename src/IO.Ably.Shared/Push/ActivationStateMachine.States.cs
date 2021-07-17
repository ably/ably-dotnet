using System.Net;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        public abstract class State
        {
            protected State(ActivationStateMachine machine)
            {
                Machine = machine;
            }

            protected ActivationStateMachine Machine { get; }

            public abstract bool Persist { get; }

            public abstract Task<State> Transition(Event @event);
        }

        public sealed class NotActivated : State
        {
            public NotActivated(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }

        // Stub for now
        public sealed class WaitingForPushDeviceDetails : State
        {
            public WaitingForPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }

        public sealed class WaitingForDeviceRegistration : State
        {
            public WaitingForDeviceRegistration(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => false;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }

        public sealed class WaitingForNewPushDeviceDetails : State
        {
            public WaitingForNewPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }

        public sealed class WaitingForDeregistration : State
        {
            private readonly State _previousState;

            public WaitingForDeregistration(ActivationStateMachine machine, State previousState)
                : base(machine)
            {
                _previousState = previousState;
            }

            public override bool Persist => false;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }

        // Stub for now
        public sealed class WaitingForRegistrationSync : State
        {
            private readonly Event _fromEvent;

            public WaitingForRegistrationSync(ActivationStateMachine machine, Event fromEvent)
                : base(machine)
            {
                _fromEvent = fromEvent;
            }

            public override bool Persist => false;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }

        public sealed class AfterRegistrationSyncFailed : State
        {
            public AfterRegistrationSyncFailed(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override async Task<State> Transition(Event @event)
            {
                return null;
            }
        }
    }
}
