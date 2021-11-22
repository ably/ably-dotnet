using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.Support.V4.Content;
using Firebase.Messaging;
using Xamarin.Essentials;

namespace IO.Ably.Push.Android
{
    public class AndroidMobileDevice : IMobileDevice
    {
        private const string TokenType = "fcm";
        private readonly ILogger _logger;
        private static AblyRealtime _realtimeInstance;

        internal AndroidMobileDevice(PushCallbacks callbacks, ILogger logger)
        {
            Callbacks = callbacks;
            _logger = logger;
        }

        /// <summary>
        /// Initialises the Android MobileDevice implementation as well initialised the AblyRealtime client that is used to subscribe to Firebase push notifications.
        /// Use this method to initialise your AblyRealtime if you want to register the device for push notifications.
        /// </summary>
        /// <param name="ablyClientOptions">ClientOptions used to initialise the AblyRealtime instanced used to setup the ActivationStat.</param>
        /// <param name="configureCallbacks">Option action which can be used to configure callbacks. It's especially useful to subscribe/unsubscribe to push channels when a device is activated / deactivated.</param>
        /// <returns>Initialised Ably instance which supports push notification registrations.</returns>
        public static IRealtimeClient Initialise(ClientOptions ablyClientOptions, Action<PushCallbacks> configureCallbacks)
        {
            var callbacks = new PushCallbacks();
            configureCallbacks?.Invoke(callbacks);
            return Initialise(ablyClientOptions, callbacks);
        }

        public static IRealtimeClient Initialise(ClientOptions ablyClientOptions, PushCallbacks callbacks = null)
        {
            var androidMobileDevice = new AndroidMobileDevice(callbacks ?? new PushCallbacks(), DefaultLogger.LoggerInstance);
            IoC.MobileDevice = androidMobileDevice;
            // Create the instance of ably used for Push registrations
            _realtimeInstance = new AblyRealtime(ablyClientOptions, androidMobileDevice);
            _realtimeInstance.Push.InitialiseStateMachine();
            return _realtimeInstance;
        }

        public static void OnNewRegistrationToken(string token)
        {
            if (_realtimeInstance is null)
            {
                throw new AblyException(
                    "No realtime instance was registered. Please initiasize your instance using AndroidMobileDevice.Initialise(options, configureCallbacks).");
            }

            var logger = _realtimeInstance.Logger;
            logger.Debug($"Received OnNewRegistrationToken with token {token}");

            var pushRealtime = _realtimeInstance.Push;
            var registrationToken = new RegistrationToken(TokenType, token);
            pushRealtime.StateMachine.UpdateRegistrationToken(Result.Ok(registrationToken));
        }

        private Context Context => Application.Context;

        public void SetPreference(string key, string value, string groupName)
        {
            Preferences.Set(key, value, groupName);
        }

        public string GetPreference(string key, string groupName)
        {
            return Preferences.Get(key, null, groupName);
        }


        public void ClearPreferences(string groupName)
        {
            Preferences.Clear(groupName);
        }

        public void RequestRegistrationToken(Action<Result<RegistrationToken>> callback)
        {
            try
            {
                _logger.Debug("Requesting a new Registration token");
                var messagingInstance = FirebaseMessaging.Instance;
                var resultTask = messagingInstance.GetToken();

                resultTask.AddOnCompleteListener(new RequestTokenCompleteListener(callback, _logger));
            }
            catch (Exception e)
            {
                _logger.Error("Error while requesting a new Registration token.", e);
                var errorInfo = new ErrorInfo($"Failed to request AndroidToken. Error: {e?.Message}.", 50000, HttpStatusCode.InternalServerError, e);
                callback(Result.Fail<RegistrationToken>(errorInfo));
            }
        }

        public PushCallbacks Callbacks { get; }
        public string DevicePlatform => "android"; // TODO: Update how we get Mobile Device.

        public string FormFactor
        {
            get
            {
                var idiom = DeviceInfo.Idiom;

                if (idiom == DeviceIdiom.Watch)
                {
                    return DeviceFormFactor.Watch;
                }

                if (idiom == DeviceIdiom.TV)
                {
                    return DeviceFormFactor.Tv;
                }

                if (idiom == DeviceIdiom.Tablet)
                {
                    return DeviceFormFactor.Tablet;
                }

                if (idiom == DeviceIdiom.Phone)
                {
                    return DeviceFormFactor.Phone;
                }

                if (idiom == DeviceIdiom.Desktop)
                {
                    return DeviceFormFactor.Desktop;
                }

                return DeviceFormFactor.Other;
            }
        }

        private class RequestTokenCompleteListener : Java.Lang.Object, IOnCompleteListener
        {
            private readonly Action<Result<RegistrationToken>> _callback;
            private readonly ILogger _logger;

            internal RequestTokenCompleteListener(Action<Result<RegistrationToken>> callback, ILogger logger)
            {
                _callback = callback;
                _logger = logger;
            }

            public void OnComplete(Task task)
            {
                if (task.IsSuccessful)
                {
                    var token = new RegistrationToken(TokenType, (string)task.Result);
                    _logger.Debug($"Token request operation completed. Token: {token.Token}");
                    _callback(Result.Ok(token));
                }
                else
                {
                    var exception = task.Exception;
                    var errorInfo = new ErrorInfo($"Failed to return valid AndroidToken. Error: {exception?.Message}.",
                        ErrorCodes.InternalError, HttpStatusCode.InternalServerError, exception);

                    _logger.Debug($"Error requesting new push notification token. Message: {errorInfo}");
                    _callback(Result.Fail<RegistrationToken>(errorInfo));
                }
            }
        }
    }
}
