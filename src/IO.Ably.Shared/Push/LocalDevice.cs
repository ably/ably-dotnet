using System;
using IO.Ably.Encryption;

namespace IO.Ably.Push
{
    /// <summary>
    /// LocalDevice represents the current state of the device in respect of it being a target for push notifications.
    /// </summary>
    public class LocalDevice : DeviceDetails
    {
        /// <summary>
        /// Devices that have completed registration have an identity token assigned to them by the push service. TODO: Check how accurate this is.
        /// </summary>
        public string DeviceIdentityToken { get; set; }

        internal bool IsRegistered => DeviceIdentityToken.IsNotEmpty();

        internal bool IsCreated => Id.IsNotEmpty();

        internal RegistrationToken GetRegistrationToken()
        {
            var recipient = Push?.Recipient;
            if (recipient != null)
            {
                return new RegistrationToken(
                    (string)recipient.GetValue("transportType"),
                    (string)recipient.GetValue("registrationToken"));
            }

            return null;
        }

        /// <summary>
        /// Create a new instance of localDevice with a random Id and secret.
        /// </summary>
        /// <param name="clientId">The clientId which is set on the device. Can be null.</param>
        /// <returns>Instance of LocalDevice</returns>
        public static LocalDevice Create(string clientId)
        {
            return new LocalDevice
            {
                Id = Guid.NewGuid().ToString("D"),
                DeviceSecret = Crypto.GenerateSecret(),
                ClientId = clientId
            };
        }
    }
}
