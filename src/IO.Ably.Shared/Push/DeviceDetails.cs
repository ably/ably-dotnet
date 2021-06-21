using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// Describes possible device form factors.
    /// </summary>
    public enum DeviceFormFactor
    {
        /// <summary>
        /// Phone
        /// </summary>
        Phone,

        /// <summary>
        /// Tablet
        /// </summary>
        Tablet,

        /// <summary>
        /// Desktop
        /// </summary>
        Desktop,

        /// <summary>
        /// Tv
        /// </summary>
        Tv,

        /// <summary>
        /// Watch
        /// </summary>
        Watch,

        /// <summary>
        /// Car
        /// </summary>
        Car,

        /// <summary>
        /// Embedded
        /// </summary>
        Embedded,

        /// <summary>
        /// Other
        /// </summary>
        Other
    }

    /// <summary>
    /// Class representing a Device registered for Ably push notifications.
    /// </summary>
    public class DeviceDetails
    {
        /// <summary>
        /// Device Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Device platform (android|ios|web). TODO: Double check.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Device form factor. <see cref="FormFactor"/>.
        /// </summary>
        public DeviceFormFactor FormFactor { get; set; }

        /// <summary>
        /// Device ClientId. TODO: Explain how this can be used to send push notifications.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Device Metadata. TODO: Give example how this can be used.
        /// </summary>
        public JObject Metadata { get; set; }

        /// <summary>
        /// Push registration data. TODO: Describe how it is relevant to each platform and how it differs.
        /// </summary>
        public PushData Push { get; set; } = new PushData();

        /// <summary>
        /// Device secret. TODO: Describe how this could be used to authenticate push messages.
        /// </summary>
        public string DeviceSecret { get; set; }

        /// <summary>
        /// Class describing Push data.
        /// </summary>
        public class PushData
        {
            /// <summary>
            /// Push Recipient. TODO: // describe the different options.
            /// </summary>
            public JObject Recipient { get; set; } // TODO: Once we know all the variations of the data, I'd like to make it strongly typed.

            /// <summary>
            /// State. TODO: Add examples.
            /// </summary>
            public string State { get; set; }

            /// <summary>
            /// Error registering device as a PushTarget.
            /// </summary>
            public ErrorInfo ErrorReason { get; set; }
        }
    }
}
