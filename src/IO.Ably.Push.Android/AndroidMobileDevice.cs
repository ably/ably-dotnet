using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.Support.V4.Content;
using Firebase.Messaging;
using IO.Ably.Infrastructure;
using Xamarin.Essentials;

namespace IO.Ably.Push.Android
{
    /// <summary>
    /// Android implementation for IMobile device.
    /// </summary>
    public class AndroidMobileDevice : IMobileDevice
    {
        /// <summary>
        /// Initialises the Android MobileDevice implementation with the IoC dependency.
        /// </summary>
        public static void Initialise()
        {
            IoC.MobileDevice = new AndroidMobileDevice(DefaultLogger.LoggerInstance);
        }

        /// <summary>
        /// Method to be called by OnNewToken which is part of a class inheriting from FirebaseMessagingService.
        /// </summary>
        /// <param name="token">The passed deviceRegistration token.</param>
        public static void OnNewRegistrationToken(string token)
        {
            var logger = DefaultLogger.LoggerInstance;
            logger.Debug($"Received OnNewRegistrationToken with token {token}");

            // TODO: Implement a way to retrieve a static StateMachine.
        }

        private readonly ILogger _logger;

        private AndroidMobileDevice(ILogger logger)
        {
            _logger = logger;
        }

        private Context Context => Application.Context;

        /// <inheritdoc/>
        public string DevicePlatform => "android";

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Please provide name when sending intent.", nameof(name));
            }

            _logger.Debug($"Sending intent {name}");

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
                throw new AblyException(e);
            }
        }

        /// <inheritdoc/>
        public void SetPreference(string key, string value, string groupName)
        {
            _logger.Debug($"Setting preferences: {groupName}:{key} with value {value}");
            Preferences.Set(key, value, groupName);
        }

        /// <inheritdoc/>
        public string GetPreference(string key, string groupName)
        {
            return Preferences.Get(key, string.Empty, groupName);
        }

        /// <inheritdoc/>
        public void RemovePreference(string key, string groupName)
        {
            _logger.Debug($"Removing preference: {groupName}:{key}");
            Preferences.Remove(key, groupName);
        }

        /// <inheritdoc/>
        public void ClearPreferences(string groupName)
        {
            _logger.Debug($"Clearing preferences group: {groupName}");
            Preferences.Clear(groupName);
        }

        /// <inheritdoc/>
        public void RequestRegistrationToken(Action<Result<string>> callback)
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
                callback(Result.Fail<string>(errorInfo));
            }
        }

        private class RequestTokenCompleteListener : Java.Lang.Object, IOnCompleteListener
        {
            private readonly Action<Result<string>> _callback;
            private readonly ILogger _logger;

            public RequestTokenCompleteListener(Action<Result<string>> callback, ILogger logger)
            {
                _callback = callback;
                _logger = logger;
            }

            public void OnComplete(Task task)
            {
                if (task.IsSuccessful)
                {
                    _logger.Debug($"RequestTaken completed successfully. Result: {task.Result}");
                    _callback(Result.Ok((string)task.Result));
                }
                else
                {
                    _logger.Error("RequestToken failed.", task.Exception);
                    var exception = task.Exception;
                    var errorInfo = new ErrorInfo(
                        $"Failed to return valid AndroidToken. Error: {exception?.Message}.",
                        50000,
                        HttpStatusCode.InternalServerError,
                        exception);
                    _callback(Result.Fail<string>(errorInfo));
                }
            }
        }
    }
}
