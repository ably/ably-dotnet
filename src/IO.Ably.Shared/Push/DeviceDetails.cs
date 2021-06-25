using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace IO.Ably.Push
{
    /// <summary>
    /// Describes possible device form factors.
    /// </summary>
    public static class DeviceFormFactor
    {
        /// <summary>
        /// Phone.
        /// </summary>
        public const string Phone = "phone";

        /// <summary>
        /// Tablet.
        /// </summary>
        public const string Tablet = "tablet";

        /// <summary>
        /// Desktop.
        /// </summary>
        public const string Desktop = "desktop";

        /// <summary>
        /// Tv.
        /// </summary>
        public const string Tv = "tv";

        /// <summary>
        /// Watch.
        /// </summary>
        public const string Watch = "watch";

        /// <summary>
        /// Car.
        /// </summary>
        public const string Car = "car";

        /// <summary>
        /// Embedded.
        /// </summary>
        public const string Embedded = "embedded";

        /// <summary>
        /// Other.
        /// </summary>
        public const string Other = "other";
    }

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
        /// Device form factor. <see cref="FormFactor"/>.
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
