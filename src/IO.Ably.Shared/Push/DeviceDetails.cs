using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// Class representing a Device registered for Ably push notifications.
    /// </summary>
    public class DeviceDetails
    {
        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Device platform (android|ios|web). TODO: Double check.
        /// </summary>
        [JsonProperty("platform")]
        public string Platform { get; set; }

        /// <summary>
        /// Device form factor.
        /// </summary>
        [JsonProperty("formFactor")]
        public string FormFactor { get; set; }

        /// <summary>
        /// Device ClientId. TODO: Explain how this can be used to send push notifications.
        /// </summary>
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Device Metadata. TODO: Give example how this can be used.
        /// </summary>
        [JsonProperty("metadata")]
        public JObject Metadata { get; set; }

        /// <summary>
        /// Push registration data. TODO: Describe how it is relevant to each platform and how it differs.
        /// </summary>
        [JsonProperty("push")]
        public PushData Push { get; set; } = new PushData();

        /// <summary>
        /// Device secret. TODO: Describe how this could be used to authenticate push messages.
        /// </summary>
        [JsonProperty("deviceSecret")]
        public string DeviceSecret { get; set; }

        /// <summary>
        /// Class describing Push data.
        /// </summary>
        public class PushData
        {
            /// <summary>
            /// Push Recipient. TODO: // describe the different options.
            /// </summary>
            [JsonProperty("recipient")]
            public JObject Recipient { get; set; } // TODO: Once we know all the variations of the data, I'd like to make it strongly typed.

            /// <summary>
            /// State. TODO: Add examples.
            /// </summary>
            [JsonProperty("state")]
            public string State { get; set; }

            /// <summary>
            /// Error registering device as a PushTarget.
            /// </summary>
            [JsonProperty("errorReason")]
            public ErrorInfo ErrorReason { get; set; }
        }
    }
}
