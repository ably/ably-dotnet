using System;
using System.Net;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        internal static readonly Func<Task<Event>> EmptyNextEventFunc =
            () => Task.FromResult((Event)null);

        internal static Func<Task<Event>> ToNextEventFunc(Func<Task<Event>> singleEventFunc)
            => async () => await singleEventFunc();

        internal static Func<Task<Event>> ToNextEventFunc(Event nextEvent)
        {
            if (nextEvent is null)
            {
                return EmptyNextEventFunc;
            }

            return async () => nextEvent;
        }

        public abstract class State
        {
            protected State(ActivationStateMachine machine)
            {
                Machine = machine;
            }

            protected ActivationStateMachine Machine { get; }

            public abstract bool Persist { get; }

            public abstract bool CanHandleEvent(Event @event);

            // Transition will return a new state for the Activation State machine to transition to
            // and a function to be executed straight after the State has transitioned.
            public abstract Task<(State, Func<Task<Event>>)> Transition(Event @event);
        }

        public sealed class NotActivated : State
        {
            public NotActivated(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledDeactivate || @event is CalledActivate;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledDeactivate _:
                        Machine.TriggerDeactivatedCallback();
                        return (this, EmptyNextEventFunc);
                    case CalledActivate _:

                        var localDevice = Machine.LocalDevice;

                        if (localDevice.IsRegistered)
                        {
                            var nextState = new WaitingForRegistrationSync(Machine, @event);
                            return (nextState, ToNextEventFunc(Machine.ValidateRegistration));
                        }

                        if (localDevice.IsCreated == false)
                        {
                            var newLocalDevice = LocalDevice.Create(Machine.ClientId, Machine._mobileDevice);
                            Machine.PersistLocalDevice(newLocalDevice);
                            Machine.LocalDevice = newLocalDevice;
                            Machine.GetRegistrationToken();

                            return (new WaitingForPushDeviceDetails(Machine), EmptyNextEventFunc);
                        }

                        var nextEvent = localDevice.RegistrationToken != null ? new GotPushDeviceDetails() : null;

                        return (new WaitingForPushDeviceDetails(Machine), ToNextEventFunc(nextEvent));
                    default:
                        throw new AblyException($"NotActivated cannot handle {@event.GetType().Name} event.", ErrorCodes.InternalError);
                }
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

            public override bool CanHandleEvent(Event @event)
            {
                throw new NotImplementedException();
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class WaitingForDeviceRegistration : State
        {
            public WaitingForDeviceRegistration(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => false;

            public override bool CanHandleEvent(Event @event)
            {
                throw new System.NotImplementedException();
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class WaitingForNewPushDeviceDetails : State
        {
            public WaitingForNewPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                throw new System.NotImplementedException();
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                throw new NotImplementedException();
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

            public override bool CanHandleEvent(Event @event)
            {
                throw new System.NotImplementedException();
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                throw new NotImplementedException();
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

            public override bool CanHandleEvent(Event @event)
            {
                throw new System.NotImplementedException();
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class AfterRegistrationSyncFailed : State
        {
            public AfterRegistrationSyncFailed(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                throw new System.NotImplementedException();
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                throw new NotImplementedException();
            }
        }
    }
}
