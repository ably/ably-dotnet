using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Ably
{
    public class StatsJsonDateConverter : DateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = reader.Value as string;
            return DateTimeOffset.ParseExact(value, "yyyy-MM-dd:HH:mm", CultureInfo.InvariantCulture);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class CapabilityJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((value as Capability).ToJson());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var capToken = JToken.Load(reader);
            return new Capability(capToken.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Capability);
        }
    }

    public class DateTimeOffsetJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var date = (DateTimeOffset)value;
            writer.WriteValue(date.ToUnixTime());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Integer)
            {
                var value = (long) token;
                if (ValueIsInMilliseconds(value))
                    return value.FromUnixTimeInMilliseconds();
                return value.FromUnixTime();
            }
            return DateTimeOffset.MinValue;
        }

        private bool ValueIsInMilliseconds(long value)
        {
            long num = (long) (value * 1000 + (value >= 0.0 ? 0.5 : -0.5));
            return num <= -315537897600000L || num >= 315537897600000L;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (DateTimeOffset) || objectType == typeof(DateTimeOffset?);
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
        public DateTimeOffset Interval { get; set; }

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
