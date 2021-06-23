using System;
using System.Net;
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
                switch (@event)
                {
                    case CalledActivate _:
                        return this;
                    case CalledDeactivate _:
                        Machine.CallDeactivatedCallback(null);
                        return new NotActivated(Machine);
                    case ActivationStateMachine.GettingPushDeviceDetailsFailed failedEvent:
                        Machine.CallDeactivatedCallback(failedEvent.Reason);
                        return new NotActivated(Machine);
                    case GotPushDeviceDetails _:
                    {
                        DeviceDetails device = Machine.LocalDevice;

                        var ably = Machine._restClient; // TODO: Check if there is an instance when Ably is not set. In which case Java set queues GettingDeviceRegistrationFailed

                        // TODO: Make async
                        try
                        {
                            var registeredDevice = await ably.Push.Admin.RegisterDevice(device);
                            var deviceIdentityToken = registeredDevice.DeviceIdentityToken;
                            if (deviceIdentityToken.IsEmpty())
                            {
                                // TODO: Log
                                _ = Machine.HandleEvent(new GettingDeviceRegistrationFailed(new ErrorInfo("Invalid deviceIdentityToken in response", 40000, HttpStatusCode.BadRequest)));
                            }
                            else
                            {
                                // TODO: When integration testing this will most likely fail. I suspect deviceIdentityToken is not a plain string.
                                _ = Machine.HandleEvent(new GotDeviceRegistration(deviceIdentityToken));

                                // TODO: RSH8f. Leaving commented out code as a reminder.
                                // I still haven't figured out how clientId in the state machine could be different from the one stored in the client
                                // JsonPrimitive responseClientIdJson = response.getAsJsonPrimitive("clientId");
                                // if (responseClientIdJson != null)
                                // {
                                //     String responseClientId = responseClientIdJson.getAsString();
                                //     if (device.clientId == null)
                                //     {
                                //         /* Spec RSH8f: there is an implied clientId in our credentials that we didn't know about */
                                //         activationContext.setClientId(responseClientId, false);
                                //     }
                                // }
                            }
                        }
                        catch (AblyException e)
                        {
                            // Log
                            _ = Machine.HandleEvent(new GettingDeviceRegistrationFailed(e.ErrorInfo));
                        }

                        return new WaitingForDeviceRegistration(Machine);
                    }

                    default:
                        return null;
                }
            }
        }

        public sealed class WaitingForDeviceRegistration : State
        {
            public override bool Persist => false;

            public WaitingForDeviceRegistration(ActivationStateMachine machine)
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
