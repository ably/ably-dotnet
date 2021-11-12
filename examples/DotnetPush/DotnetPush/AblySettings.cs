using Xamarin.Essentials;

namespace DotnetPush
{
    /// <summary>
    /// Helper class to access saved settings.
    /// </summary>
    public static class AblySettings
    {
        /// <summary>
        /// ClientId.
        /// </summary>
        public static string ClientId
        {
            get => Preferences.Get("ABLY_CLIENT_ID", string.Empty, "Ably_Device");
            set => Preferences.Set("ABLY_CLIENT_ID", value, "Ably_Device");
        }

        /// <summary>
        /// Ably key.
        /// </summary>
        public static string AblyKey
        {
            get => Preferences.Get("ABLY_AUTH_KEY", string.Empty, "Ably_Device");
            set => Preferences.Set("ABLY_AUTH_KEY", value, "Ably_Device");
        }
    }
}
