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
    public class AndroidMobileDevice : IMobileDevice
    {
        public static void Initialise()
        {
            IoC.MobileDevice = new AndroidMobileDevice(null);
        }

        public static void OnNewRegistrationToken(string token)
        {
            //Get Ably and the ActivationStateMachine
        }

        private readonly ILogger _logger;

        internal AndroidMobileDevice(ILogger logger)
        {
            _logger = logger;
        }

        private Context Context => Application.Context;

        public string DevicePlatform => "android";

        public string FormFactor
        {
            get
            {
                var idiom = DeviceInfo.Idiom;
                if(idiom == DeviceIdiom.Watch)
                {
                    return DeviceFormFactor.Watch;
                }
                if(idiom == DeviceIdiom.TV)
                {
                    return DeviceFormFactor.Tv;
                }
                if(idiom == DeviceIdiom.Tablet)
                {
                    return DeviceFormFactor.Tablet;
                }
                if(idiom == DeviceIdiom.Phone)
                {
                    return DeviceFormFactor.Phone;
                }
                if(idiom == DeviceIdiom.Desktop)
                {
                    return DeviceFormFactor.Desktop;
                }

                return DeviceFormFactor.Other;
            }
        }

        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            if (string.IsNullOrEmpty(name))
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
            return Preferences.Get(key, "", groupName);
        }

        public void RemovePreference(string key, string groupName)
        {
            Preferences.Remove(key, groupName);
        }

        public void ClearPreferences(string groupName)
        {
            Preferences.Clear(groupName);
        }

        public void RequestRegistrationToken(Action<Result<string>> callback)
        {
            try
            {
                var messagingInstance = FirebaseMessaging.Instance;
                var resultTask = messagingInstance.GetToken();

                resultTask.AddOnCompleteListener(new RequestTokenCompleteListener(callback));
            }
            catch (Exception e)
            {
                // TODO: Log
                var errorInfo = new ErrorInfo($"Failed to request AndroidToken. Error: {e?.Message}.", 50000,
                    HttpStatusCode.InternalServerError, e);
                callback(Result.Fail<string>(errorInfo));
            }
        }


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
                        50000, HttpStatusCode.InternalServerError, exception);
                    _callback(Result.Fail<string>(errorInfo));
                }
            }
        }
    }
}