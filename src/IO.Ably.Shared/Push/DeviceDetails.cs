using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    public enum DeviceFormFactor
    {
        Phone,
        Tablet,
        Desktop,
        Tv,
        Watch,
        Car,
        Embedded,
        Other
    }

    public class DeviceDetails {
        public string Id { get; set; }

        public string Platform { get; set; }

        public DeviceFormFactor FormFactor { get; set; }

        public string ClientId { get; set; }

        public JObject Metadata { get; set; }

        public PushData Push { get; set; } = new PushData();

        public string DeviceSecret { get; set; }

        public class PushData
        {
            // TODO: Once all the variations are known make it strongly typed
            public JObject Recipient { get; set; }

            public string State { get; set; }

            public ErrorInfo ErrorReason { get; set; }
        }
    }
}