using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ably
{
    public class StatsJsonDateConverter : DateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value as string;
            return DateTime.ParseExact(value, "yyyy-MM-dd:HH:mm", CultureInfo.InvariantCulture);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class Stats
    {
        public MessageTypes All { get; set; }
        public MessageTraffic Inbound { get; set; }
        public MessageTraffic Outbound { get; set; }
        public MessageTypes Persisted { get; set; }
        public ConnectionTypes Connections { get; set; }
        public ResourceCount Channels { get; set; }
        public RequestCount ApiRequests { get; set; }
        public RequestCount TokenRequests { get; set; }
        [JsonProperty("intervalId")]
        [JsonConverter(typeof(StatsJsonDateConverter))]
        public DateTime Interval { get; set; }

    public Stats()
        {
            All = new MessageTypes();
            Inbound = new MessageTraffic();
            Outbound = new MessageTraffic();
            Persisted = new MessageTypes();
            Connections = new ConnectionTypes();
            Channels = new ResourceCount();
            ApiRequests = new RequestCount();
            TokenRequests = new RequestCount();
        }
    }

    public class ConnectionTypes
    {
        public ResourceCount All { get; set; }
        public ResourceCount Plain { get; set; }
        public ResourceCount Tls { get; set; }

        public ConnectionTypes()
        {
            All = new ResourceCount();
            Plain = new ResourceCount();
            Tls = new ResourceCount();
        }
    }

    public class MessageCount
    {
        public double Count { get; set; }
        public double Data { get; set; }
    }

    /**
     * A breakdown of summary stats data for different (message vs presence)
     * message types.
     */
    public class MessageTypes
    {
        public MessageCount All { get; set; }
        public MessageCount Messages { get; set; }
        public MessageCount Presence { get; set; }

        public MessageTypes()
        {
            All = new MessageCount();
            Messages = new MessageCount();
            Presence = new MessageCount();
        }
    }

    /**
     * A breakdown of summary stats data for traffic over various transport types.
     */
    public class MessageTraffic
    {
        public MessageTypes All { get; set; }
        public MessageTypes Realtime { get; set; }
        public MessageTypes Rest { get; set; }
        public MessageTypes Push { get; set; }
        public MessageTypes HttpStream { get; set; }

        public MessageTraffic()
        {
            All = new MessageTypes();
            Realtime = new MessageTypes();
            Rest = new MessageTypes();
            Push = new MessageTypes();
            HttpStream = new MessageTypes();
        }
    }

    public class RequestCount
    {
        public double Succeeded { get; set; }
        public double Failed { get; set; }
        public double Refused { get; set; }
    }

    public class ResourceCount
    {
        public double Opened { get; set; }
        public double Peak { get; set; }
        public double Mean { get; set; }
        public double Min { get; set; }
        public double Refused { get; set; }
    }
}
