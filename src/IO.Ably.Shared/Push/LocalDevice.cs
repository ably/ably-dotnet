using System;
using IO.Ably.Encryption;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// LocalDevice represents the current state of the device in respect of it being a target for push notifications.
    /// </summary>
    public class LocalDevice : DeviceDetails
    {
        /// <summary>
        /// Devices that have completed registration have an identity token assigned to them by the push service.
        /// It can be used to authenticate Push Admin requests.
        /// </summary>
        [JsonIgnore]
        public string DeviceIdentityToken { get; set; }

        internal bool IsRegistered => DeviceIdentityToken.IsNotEmpty();

        internal bool IsCreated => Id.IsNotEmpty();

        internal RegistrationToken RegistrationToken
        {
            get
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

            set
            {
                if (value != null)
                {
                    JObject obj = new JObject();
                    obj.Add("transportType", value.Type);
                    obj.Add("registrationToken", value.Token);
                    Push.Recipient = obj;
                }
            }
        }

        /// <summary>
        /// Create a new instance of localDevice with a random Id and secret.
        /// </summary>
        /// <param name="clientId">Optional clientId which is set on the device.</param>
        /// <returns>Instance of LocalDevice.</returns>
        public static LocalDevice Create(string clientId = null)
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
