using System;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        public abstract class State
        {
            protected ActivationStateMachine Machine { get; }

            public State(ActivationStateMachine machine)
            {
                Machine = machine;
            }

            public abstract bool Persist { get; }

            public abstract Task<State> Transition(ActivationStateMachine.Event @event);
        }

        public sealed class NotActivated : State
        {
            public override bool Persist => true;

            public NotActivated(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override async Task<State> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledDeactivate _:
                        Machine.CallDeactivatedCallback(null);
                        return this;
                    case CalledActivate _:
                        // TODO: Logging
                        var device = Machine.LocalDevice;

                        if (device.IsRegistered)
                        {
                            await Machine.ValidateRegistration();
                            return new WaitingForRegistrationSync(Machine, @event);
                        }

                        if (device.RegistrationToken != null)
                        {
                            Machine.AddToEventQueue(new GotPushDeviceDetails());
                        }
                        else
                        {
                            Machine.GetRegistrationToken();
                        }

                        if (device.IsCreated == false)
                        {
                            var newLocalDevice = LocalDevice.Create(Machine.ClientId);
                            Machine.PersistLocalDevice(newLocalDevice);
                            Machine.LocalDevice = newLocalDevice;
                        }

                        return new WaitingForPushDeviceDetails(Machine);
                    case GotPushDeviceDetails _:
                        return this;
                    default:
                        return null;
                }
            }
        }

        // Stub for now
        public sealed class WaitingForPushDeviceDetails : State
        {
            public override bool Persist => true;

            public WaitingForPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override async Task<State> Transition(Event @event)
            {
                throw new NotImplementedException();
            }
        }

        // Stub for now
        public sealed class WaitingForRegistrationSync : State
        {
            public override bool Persist => false;

            private ActivationStateMachine.Event _fromEvent;

            public WaitingForRegistrationSync(ActivationStateMachine machine, Event fromEvent)
                : base(machine)
            {
                _fromEvent = fromEvent;
            }

            public override async Task<State> Transition(Event @event)
            {
                throw new NotImplementedException();
            }
        }
    }
}
