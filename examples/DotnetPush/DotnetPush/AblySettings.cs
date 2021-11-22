using Xamarin.Essentials;

namespace DotnetPush
{
    /// <summary>
    /// Helper class to access saved settings.
    /// </summary>
    public static class AblySettings
    {
        private const string AuthKeyPreferenceKey = "ABLY_AUTH_KEY";
        private const string ClientIdPreferenceKey = "ABLY_CLIENT_ID";

        /// <summary>
        /// ClientId.
        /// </summary>
        public static string ClientId
        {
            get => Preferences.Get(ClientIdPreferenceKey, string.Empty, "Ably_Device");
            set => Preferences.Set(ClientIdPreferenceKey, value, "Ably_Device");
        }

        /// <summary>
        /// Ably key.
        /// </summary>
        public static string AblyKey
        {
            get => Preferences.Get(AuthKeyPreferenceKey, string.Empty, "Ably_Device");
            set => Preferences.Set(AuthKeyPreferenceKey, value, "Ably_Device");
        }
    }
}
