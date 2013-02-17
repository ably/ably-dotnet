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
    }

    public class ResourceCount
    {
        public int? Opened { get; set;}
        public int? Peak { get; set; }
        public int? Mean { get; set; }
        public int? Min { get; set; }
        public int? Refused { get; set; } 
    }

    public  class RequestCount
    {
        public int? Succeeded { get; set; }
        public int? Failed { get; set; }
        public int? Refused { get; set; }
    }

    public class MessageTypes
    {
        public MessageCount All { get; set; }
        public MessageCount Messages { get; set; }
        public MessageCount Presence { get; set; }
    }
     
    public class MessageCount
    {
        public int? Count { get; set; }
        public double? Data { get; set; }
    }

    public class MessageTraffic
    {
        public MessageTypes All { get; set; }
        public MessageTypes Realtime { get; set; }
        public MessageTypes Rest { get; set; }
        public MessageTypes Post { get; set; }
        public MessageTypes HttpStream { get; set; }
    }

    public class ConnectionTypes
    {
        public ResourceCount All { get; set; }
        public ResourceCount Plain { get; set; }
        public ResourceCount Tls { get; set; }
    }
}
