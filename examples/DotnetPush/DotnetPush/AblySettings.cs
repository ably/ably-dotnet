using Xamarin.Essentials;

namespace DotnetPush
{
    /// <summary>
    /// Helper class to access saved settings.
    /// </summary>
    public static class AblySettings
    {
        private const string ClientIdPreferenceKey = "ABLY_CLIENT_ID";

        /// <summary>
        /// ClientId.
        /// </summary>
        public static string ClientId
        {
            get => Preferences.Get(ClientIdPreferenceKey, string.Empty);
            set => Preferences.Set(ClientIdPreferenceKey, value);
        }
    }
}
