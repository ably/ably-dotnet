using System;
using System.Collections.Generic;
using System.Net;
using Android.Gms.Tasks;
using Firebase.Messaging;
using Xamarin.Essentials;

namespace IO.Ably.Push.Android
{
    public class AndroidMobileDevice : IMobileDevice
    {
        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            throw new System.NotImplementedException();
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
                var errorInfo = new ErrorInfo($"Failed to request AndroidToken. Error: {e?.Message}.", 50000, HttpStatusCode.InternalServerError, e);
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
                    var errorInfo = new ErrorInfo($"Failed to return valid AndroidToken. Error: {exception?.Message}.", 50000, HttpStatusCode.InternalServerError, exception);
                    _callback(Result.Fail<string>(errorInfo));
                }
            }

        }

    }
}