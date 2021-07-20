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
                return @event is CalledDeactivate || @event is CalledActivate || @event is GotPushDeviceDetails;
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
                    case GotPushDeviceDetails _:
                        return (this, EmptyNextEventFunc);
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
                return @event is CalledActivate
                       || @event is CalledDeactivate
                       || @event is GotPushDeviceDetails
                       || @event is GettingDeviceRegistrationFailed;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        return (this, EmptyNextEventFunc);
                    case CalledDeactivate _:
                        Machine.TriggerDeactivatedCallback();
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    case GettingDeviceRegistrationFailed failedEvent:
                        Machine.TriggerActivatedCallback(failedEvent.Reason);
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    case GotPushDeviceDetails _:
                        return (new WaitingForDeviceRegistration(Machine), ToNextEventFunc(RegisterDevice));
                    default:
                        throw new AblyException($"WaitingForPushDeviceDetails cannot handle {@event.GetType().Name} event.", ErrorCodes.InternalError);
                }

                async Task<Event> RegisterDevice()
                {
                    DeviceDetails device = Machine.LocalDevice;

                    var ably = Machine._restClient; // TODO: Check if there is an instance when Ably is not set. In which case Java set queues GettingDeviceRegistrationFailed

                    try
                    {
                        var registeredDevice = await ably.Push.Admin.RegisterDevice(device);
                        var deviceIdentityToken = registeredDevice.DeviceIdentityToken;
                        if (deviceIdentityToken.IsEmpty())
                        {
                            return new GettingDeviceRegistrationFailed(new ErrorInfo(
                                "Invalid deviceIdentityToken in response", ErrorCodes.BadRequest, HttpStatusCode.BadRequest));
                        }

                        return new GotDeviceRegistration(deviceIdentityToken);
                    }
                    catch (AblyException e)
                    {
                        return new GettingDeviceRegistrationFailed(e.ErrorInfo);
                    }
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

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate
                       || @event is GotDeviceRegistration
                       || @event is GettingDeviceRegistrationFailed;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        return (this, EmptyNextEventFunc);
                    case GotDeviceRegistration registrationEvent:
                        Machine.SetDeviceIdentityToken(registrationEvent.DeviceIdentityToken);
                        Machine.TriggerActivatedCallback();
                        return (new WaitingForNewPushDeviceDetails(Machine), EmptyNextEventFunc);
                    case GettingDeviceRegistrationFailed failedEvent:
                        Machine.TriggerActivatedCallback(failedEvent.Reason);
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    default:
                        throw new AblyException($"WaitingForDeviceRegistration cannot handle {@event.GetType().Name} event.", ErrorCodes.InternalError);
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

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate
                    || @event is CalledDeactivate
                    || @event is GotPushDeviceDetails;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        Machine.TriggerActivatedCallback();
                        return (this, EmptyNextEventFunc);
                    case CalledDeactivate _:
                        var localDevice = Machine.LocalDevice;
                        return (new WaitingForDeregistration(Machine, this), ToNextEventFunc(() => Deregister(localDevice.Id)));
                    case GotPushDeviceDetails _:
                        var device = Machine.EnsureLocalDeviceIsLoaded();

                        return (new WaitingForRegistrationSync(Machine, @event), ToNextEventFunc(() => UpdateRegistration(device)));
                    default:
                        throw new AblyException($"WaitingForNewPushDeviceDetails cannot handle {@event.GetType().Name} event.", ErrorCodes.InternalError);
                }

                async Task<Event> Deregister(string deviceId)
                {
                    try
                    {
                        await Machine._restClient.Push.Admin.DeviceRegistrations.RemoveAsync(deviceId);
                        return new Deregistered();
                    }
                    catch (AblyException e)
                    {
                        return new DeregistrationFailed(e.ErrorInfo);
                    }
                }

                async Task<Event> UpdateRegistration(DeviceDetails details)
                {
                    try
                    {
                        Machine.Debug($"Updating device registration {details.ToJson()}");
                        await Machine._restClient.Push.Admin.PatchDeviceRecipient(details);
                        return new RegistrationSynced();
                    }
                    catch (AblyException ex)
                    {
                        Machine.Error($"Error updating Registration. DeviceDetails: {details.ToJson()}", ex);
                        return new SyncRegistrationFailed(ex.ErrorInfo);
                    }
                }
            }
        }

        public sealed class WaitingForDeregistration : State
        {
            public State PreviousState { get; }

            public WaitingForDeregistration(ActivationStateMachine machine, State previousState)
                : base(machine)
            {
                PreviousState = previousState;
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
            public Event FromEvent { get; }

            public WaitingForRegistrationSync(ActivationStateMachine machine, Event fromEvent)
                : base(machine)
            {
                FromEvent = fromEvent;
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
