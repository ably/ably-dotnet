using System;
using System.Collections.Generic;

namespace IO.Ably.Push
{
    /// <summary>
    /// Interface for communicating with a mobile device supporting pushing notifications.
    /// </summary>
    public interface IMobileDevice
    {
        /// <summary>
        /// Trigger Intent(Android) or ... (Apple).
        /// </summary>
        /// <param name="name">name of the intent. It will be prepended with io.ably.broadcast..</param>
        /// <param name="extraParameters">extra parameters set for the intent.</param>
        void SendIntent(string name, Dictionary<string, object> extraParameters);

        /// <summary>
        /// Persist a preferences on the mobile device. TODO: Add info specific to Android and iOS.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="groupName">Groups preferences so they can be removed easier.</param>
        void SetPreference(string key, string value, string groupName);

        /// <summary>
        /// Retrieves a preference from the mobile device.
        /// </summary>
        /// <param name="key">Preference Key.</param>
        /// <param name="groupName">Group name.</param>
        /// <returns>The value of the preference or null if it doesn't exist.</returns>
        string GetPreference(string key, string groupName);

        /// <summary>
        /// Deletes a preference from the mobile device if it exists.
        /// </summary>
        /// <param name="key">Preference key.</param>
        /// <param name="groupName">Group the key belongs to.</param>
        void RemovePreference(string key, string groupName);

        /// <summary>
        /// Remove a whole group of preferences.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        void ClearPreferences(string groupName);

        /// <summary>
        /// Requests a registration token. So far used only by Android.
        /// </summary>
        /// <param name="callback">Action which is executed when the operation completes.</param>
        void RequestRegistrationToken(Action<Result<string>> callback);
    }
}
