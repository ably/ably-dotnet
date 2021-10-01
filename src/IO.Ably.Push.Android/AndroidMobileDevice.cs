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
        public static IRealtimeClient Initialise(ClientOptions ablyClientOptions, Action<PushCallbacks> configureCallbacks = null)
        {
            var callbacks = new PushCallbacks();
            configureCallbacks?.Invoke(callbacks);
            var androidMobileDevice = new AndroidMobileDevice(callbacks, DefaultLogger.LoggerInstance);
            IoC.MobileDevice = androidMobileDevice;
            // Create the instance of ably used for Push registrations
            _realtimeInstance = new AblyRealtime(ablyClientOptions, androidMobileDevice);
            _realtimeInstance.Push.InitialiseStateMachine();
            return _realtimeInstance;
        }

        private Context Context => Application.Context;

        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            if (name.IsNotEmpty())
            {
                throw new ArgumentException("Please provide name when sending intent.", nameof(name));
            }

            var action = "io.ably.broadcast." + name.ToLower();
            try
            {
                Intent intent = new Intent(action);
                if (extraParameters.Any())
                {
                    foreach (var pair in extraParameters)
                    {
                        intent.PutExtra(pair.Key, pair.Value.ToString());
                    }
                }

                LocalBroadcastManager.GetInstance(Context).SendBroadcast(intent);
            }
            catch (Exception e)
            {
                _logger.Error($"Error sending intent {action}", e);
                // Log
                throw new AblyException(e);
            }
        }

        public void SetPreference(string key, string value, string groupName)
        {
            Preferences.Set(key, value, groupName);
        }

        public string GetPreference(string key, string groupName)
        {
            return Preferences.Get(key, groupName);
        }

        public void RemovePreference(string key, string groupName)
        {
            Preferences.Remove(key, groupName);
        }

        public void ClearPreferences(string groupName)
        {
            Preferences.Clear(groupName);
        }

        public void RequestRegistrationToken(Action<Result<RegistrationToken>> callback)
        {
            throw new NotImplementedException();
        }

        public PushCallbacks Callbacks { get; }
        public string DevicePlatform { get; } = "android"; // TODO: Update how we get Mobile Device.
        public string FormFactor { get; } = "tbc"; // TODO: Update how we pull form factor.

        public class RequestTokenCompleteListener : Java.Lang.Object, IOnCompleteListener
        {
            private readonly Action<Result<string>> _callback;

            public RequestTokenCompleteListener(Action<Result<string>> callback)
            {
                _callback = callback;
            }

            public void OnComplete(Task task)
            {
                if (task.IsSuccessful)
                {
                    _callback(Result.Ok((string) task.Result));
                }
                else
                {
                    // TODO: Log
                    var exception = task.Exception;
                    var errorInfo = new ErrorInfo($"Failed to return valid AndroidToken. Error: {exception?.Message}.",
                        ErrorCodes.InternalError, HttpStatusCode.InternalServerError, exception);
                    _callback(Result.Fail<string>(errorInfo));
                }
            }
        }
    }
}
