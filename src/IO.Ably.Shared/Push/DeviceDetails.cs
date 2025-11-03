using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// Class representing a Device registered for Ably push notifications.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class DeviceDetails
    {
        /// <summary>
        /// Device Id.
        /// </summary>
        [Key("id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Device platform. One of 'android', 'ios' or 'browser').
        /// </summary>
        [Key("platform")]
        [JsonProperty("platform")]
        public string Platform { get; set; }

        /// <summary>
        /// Device form factor. One of 'phone', 'tablet', 'desktop', 'tv', 'watch', 'car' or 'embedded'.
        /// </summary>
        [Key("formFactor")]
        [JsonProperty("formFactor")]
        public string FormFactor { get; set; }

        /// <summary>
        /// Device ClientId which is associated with the push registration.
        /// </summary>
        [Key("clientId")]
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Device Metadata. It's a flexible key value pair. Usually used to tag devices.
        /// </summary>
        [Key("metadata")]
        [JsonProperty("metadata")]
        public JObject Metadata { get; set; }

        /// <summary>
        /// Push registration data.
        /// </summary>
        [Key("push")]
        [JsonProperty("push")]
        public PushData Push { get; set; } = new PushData();

        /// <summary>
        /// Random string which is automatically generated when a new LocalDevice is created and can be used to authenticate PushAdmin Rest requests.
        /// </summary>
        [Key("deviceSecret")]
        [JsonProperty("deviceSecret")]
        public string DeviceSecret { get; set; }

        /// <summary>
        /// Class describing Push data.
        /// </summary>
        [MessagePackObject(keyAsPropertyName: true)]
        public class PushData
        {
            /// <summary>
            /// Push Recipient. Currently supporter recipients are Apple (apns), Google (fcm) and Browser (web).
            /// For more information - https://ably.com/docs/rest-api#post-device-registration.
            /// </summary>
            [Key("recipient")]
            [JsonProperty("recipient")]
            public JObject Recipient { get; set; }

            /// <summary>
            /// State of the push integration.
            /// </summary>
            [Key("state")]
            [JsonProperty("state")]
            public string State { get; set; }

            /// <summary>
            /// Error registering device as a PushTarget.
            /// </summary>
            [Key("errorReason")]
            [JsonProperty("errorReason")]
            public ErrorInfo ErrorReason { get; set; }
        }
    }
}
