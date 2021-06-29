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
                            var newLocalDevice = LocalDevice.Create(Machine.ClientId, Machine._mobileDevice);
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
            public WaitingForPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

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
            public WaitingForDeviceRegistration(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => false;

            public override async Task<State> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        return this;
                    case GotDeviceRegistration registrationEvent:
                        Machine.SetDeviceIdentityToken(registrationEvent.DeviceIdentityToken);
                        Machine.CallActivatedCallback(null);
                        return new WaitingForNewPushDeviceDetails(Machine);
                    case GettingDeviceRegistrationFailed failedEvent:
                        Machine.CallActivatedCallback(failedEvent.Reason);
                        return new NotActivated(Machine);
                    default:
                        return null;
                }
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
                switch (@event)
                {
                    case CalledActivate _:
                        Machine.CallActivatedCallback(null);
                        return this;
                    case CalledDeactivate _:
                        // We don't want to wait for the call to complete
                        // which is why I'm not awaiting it.
                        _ = Machine.Deregister();

                        return new WaitingForDeregistration(Machine, this);
                    case GotPushDeviceDetails _:
                        // Note: I don't fully understand why we do this.
                        var device = Machine.EnsureLocalDeviceIsLoaded();

                        _ = Machine.UpdateRegistration(device);

                        return new WaitingForRegistrationSync(Machine, @event);
                    default:
                        return null;
                }
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
                switch (@event)
                {
                    case CalledDeactivate _:
                        return this;
                    case Deregistered _:

                        Machine.ResetDevice();
                        Machine.CallDeactivatedCallback(null);
                        return new NotActivated(Machine);
                    case DeregistrationFailed failed:
                        Machine.CallDeactivatedCallback(failed.Reason);
                        return _previousState;
                    default:
                        return null;
                }
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
                switch (@event)
                {
                    case CalledActivate _ when _fromEvent is CalledActivate:
                        // Don't handle; there's a CalledActivate ongoing already, so this one should
                        // be enqueued for when that one finishes.
                        return null;

                    case CalledActivate _:
                        Machine.CallActivatedCallback(null);
                        return this;

                    case RegistrationSynced _:
                        if (_fromEvent is CalledActivate)
                        {
                            Machine.CallActivatedCallback(null);
                        }

                        return new WaitingForNewPushDeviceDetails(Machine);

                    case SyncRegistrationFailed failed:
                        // TODO: Here we could try to recover ourselves if the error is e. g.
                        // a networking error. Just notify the user for now.
                        ErrorInfo reason = failed.Reason;
                        if (_fromEvent is CalledActivate)
                        {
                            Machine.CallActivatedCallback(reason);
                        }
                        else
                        {
                            Machine.CallSyncRegistrationFailedCallback(reason);
                        }

                        return new AfterRegistrationSyncFailed(Machine);

                    default:
                        return null;
                }
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
                switch (@event)
                {
                    case CalledActivate _:
                    case GotPushDeviceDetails _:
                        _ = Machine.ValidateRegistration();
                        return new WaitingForRegistrationSync(Machine, @event);
                    case CalledDeactivate _:
                        _ = Machine.Deregister();
                        return new WaitingForDeregistration(Machine, this);
                    default:
                        return null;
                }
            }
        }
    }
}
