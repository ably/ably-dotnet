﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Utils;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private readonly SemaphoreSlim _handleEventsLock = new SemaphoreSlim(1, 1);
        private readonly AblyRest _restClient;
        private readonly ILogger _logger;
        private readonly Action<string, string> _stateChangeHandler = (currentState, newState) => { };
        private State _currentState;

        public string ClientId { get; }

        public IMobileDevice MobileDevice => _restClient.MobileDevice;

        internal Action<Event> ProcessingEventCallback { get; set; } = (@event) => { };

        public State CurrentState
        {
            get => _currentState;
            internal set
            {
                if (value != null && ReferenceEquals(value, _currentState) == false)
                {
                    _stateChangeHandler(_currentState?.GetType().Name, value.GetType().Name);
                }

                _currentState = value;
            }
        }

        internal Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        internal ActivationStateMachine(AblyRest restClient, ILogger logger = null)
        {
            _restClient = restClient;
            ClientId = _restClient.Auth.ClientId;
            _logger = logger ?? restClient.Logger;
        }

        public LocalDevice LocalDevice
        {
            get => _restClient.Device;
            internal set => _restClient.Device = value; // For testing purposes only
        }

        public async Task HandleEvent(Event @event)
        {
            // Handle current event and any consequent events
            // if current event didn't change state put it in pending queue and return
            // finally check the pending event queue and process it following the above rules
            // finally finally - preserve the state
            Debug($"Handling event ({@event.GetType().Name}. CurrentState: {CurrentState.GetType().Name}");

            var canEnter = await _handleEventsLock.WaitAsync(2000); // Arbitrary number = 2 second
            if (canEnter)
            {
                try
                {
                    ProcessingEventCallback(@event);
                    await HandleInner();
                    PersistState();
                }
                catch (Exception exception)
                {
                    _logger.Error("Error processing handle event.", exception);
                    throw;
                }
                finally
                {
                    _handleEventsLock.Release();
                }
            }
            else
            {
                Debug("Failed to acquire HandleEvent lock.");
            }

            // I've not made these static because it make it easier to access the debug and error logging methods
            async Task<State> GetNextState(State currentState, Event eventToProcess, Queue<Event> pendingQueue)
            {
                if (eventToProcess is null)
                {
                    return currentState;
                }

                if (currentState.CanHandleEvent(eventToProcess) == false)
                {
                    Debug("No next state returned. Queuing event for later execution.");

                    pendingQueue.Enqueue(eventToProcess);
                    return currentState;
                }

                var (nextState, nextEventFunc) = await currentState.Transition(eventToProcess);

                if (nextState == null || ReferenceEquals(nextState, currentState))
                {
                    return currentState;
                }

                var nextEvent = await nextEventFunc();

                if (nextEvent is null)
                {
                    return nextState;
                }

                return await GetNextState(nextState, nextEvent, pendingQueue);
            }

            async Task HandleInner()
            {
                CurrentState = await GetNextState(CurrentState, @event, PendingEvents);

                // Once we have updated the state we can get the next event which came from the Update
                // and try to transition the state again.
                while (PendingEvents.Any())
                {
                    Event pendingEvent = PendingEvents.Peek();
                    if (pendingEvent is null)
                    {
                        break;
                    }

                    if (CurrentState.CanHandleEvent(pendingEvent))
                    {
                        Debug($"Processing pending event ({pendingEvent.GetType().Name}. CurrentState: {CurrentState.GetType().Name}");

                        // Update the current state based on the event we got.
                        CurrentState = await GetNextState(CurrentState, pendingEvent, PendingEvents);
                        _ = PendingEvents.Dequeue(); // Remove the message from the queue.
                    }
                    else
                    {
                        Debug(
                            $"({pendingEvent.GetType().Name} can't be handled by currentState: {CurrentState.GetType().Name}");
                        break;
                    }
                }
            }
        }

        private void PersistState()
        {
            Debug(
                $"Persisting State and PendingQueue. State: {CurrentState.GetType().Name}. Queue: {PendingEvents.Select((x, i) => $"({i}) {x.GetType().Name}").JoinStrings()}");

            if (CurrentState != null && CurrentState.Persist)
            {
                MobileDevice.SetPreference(PersistKeys.StateMachine.CurrentState, CurrentState.GetType().Name, PersistKeys.StateMachine.SharedName);
            }

            var events = PendingEvents.ToList();

            // Saves pending events as a pipe separated list.
            MobileDevice.SetPreference(PersistKeys.StateMachine.PendingEvents, events.Select(x => x.GetType().Name).JoinStrings("|"), PersistKeys.StateMachine.SharedName);
        }

        private void TriggerDeactivatedCallback(ErrorInfo reason = null)
        {
            if (MobileDevice.Callbacks.DeactivatedCallback != null)
            {
                _ = NotifyExternalClient(
                    () => MobileDevice.Callbacks.DeactivatedCallback(reason),
                    nameof(MobileDevice.Callbacks.DeactivatedCallback));
            }
        }

        private void TriggerActivatedCallback(ErrorInfo reason = null)
        {
            if (MobileDevice.Callbacks.ActivatedCallback != null)
            {
                NotifyExternalClient(
                    () => MobileDevice.Callbacks.ActivatedCallback(reason),
                    nameof(MobileDevice.Callbacks.ActivatedCallback));
            }
        }

        private void TriggerSyncRegistrationFailedCallback(ErrorInfo reason)
        {
            if (MobileDevice.Callbacks.SyncRegistrationFailedCallback != null)
            {
                NotifyExternalClient(
                    () => MobileDevice.Callbacks.SyncRegistrationFailedCallback(reason),
                    nameof(MobileDevice.Callbacks.SyncRegistrationFailedCallback));
            }
        }

        private Task NotifyExternalClient(Func<Task> action, string reason)
        {
            try
            {
                Debug($"Triggering callback {reason}");
                return Task.Run(() => ActionUtils.SafeExecute(action));
            }
            catch (Exception e)
            {
                Error("Error while notifying external client for " + reason, e);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task<Event> ValidateRegistration()
        {
            Debug("Validating Registration");

            var presentClientId = _restClient.Auth.ClientId;
            if (presentClientId.IsNotEmpty() && LocalDevice.ClientId.IsNotEmpty() &&
                presentClientId.EqualsTo(LocalDevice.ClientId) == false)
            {
                Debug(
                    $"Activation failed. Auth clientId '{presentClientId}' is not equal to Device clientId '{LocalDevice.ClientId}'");

                var error = new ErrorInfo(
                    "Activation failed: present clientId is not compatible with existing device registration",
                    ErrorCodes.ActivationFailedClientIdMismatch,
                    HttpStatusCode.BadRequest);

                return new SyncRegistrationFailed(error);
            }

            try
            {
                await _restClient.Push.Admin.DeviceRegistrations.SaveAsync(LocalDevice);

                return new RegistrationSynced();
            }
            catch (AblyException e)
            {
                Error("Error validating registration", e);
                return new SyncRegistrationFailed(e.ErrorInfo);
            }
        }

        private void Debug(string message) => _logger.Debug($"ActivationStateMachine: {message}");

        private void Error(string message, Exception ex) => _logger.Error($"ActivationStateMachine: {message}", ex);

        private void ResetDevice()
        {
            LocalDevice.ResetDevice(MobileDevice);
        }

        private void GetRegistrationToken()
        {
            MobileDevice.RequestRegistrationToken(UpdateRegistrationToken);
        }

        public void UpdateRegistrationToken(Result<RegistrationToken> tokenResult)
        {
            throw new NotImplementedException();
        }

        private void SetDeviceIdentityToken(string deviceIdentityToken)
        {
            LocalDevice.DeviceIdentityToken = deviceIdentityToken;
            MobileDevice.SetPreference(PersistKeys.Device.DeviceToken, deviceIdentityToken, PersistKeys.Device.SharedName);
        }
    }
}
