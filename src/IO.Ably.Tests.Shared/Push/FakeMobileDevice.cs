using System;
using System.Collections.Generic;
using IO.Ably.Push;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public class FakeMobileDevice : IMobileDevice
    {
        public Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            throw new NotImplementedException();
        }

        public void SetPreference(string key, string value, string groupName)
        {
            Settings[$"{groupName}:{key}"] = value;
        }

        public string GetPreference(string key, string groupName)
        {
            return Settings[$"{groupName}:{key}"];
        }

        public void RemovePreference(string key, string groupName)
        {
            Settings.Remove($"{groupName}:{key}");
        }

        public void ClearPreferences(string groupName)
        {
            var keysToRemove = new List<string>();
            foreach (var key in Settings.Keys)
            {
                if (key.StartsWith(groupName))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                Settings.Remove(key);
            }
        }

        public void RequestRegistrationToken(Action<Result<string>> callback)
        {
            throw new NotImplementedException();
        }

        public PushCallbacks Callbacks { get; } = new PushCallbacks();

        public string DevicePlatform => "test";

        public string FormFactor => "phone";
    }
}
