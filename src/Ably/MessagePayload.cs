using Newtonsoft.Json;

namespace Ably
{
    public class MessagePayload
    {
        public string name { get; set; }

        public object data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string encoding { get; set; }

        public long? timestamp { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Data: {1}, Encoding: {2}, Timestamp: {3}", name, data, encoding, timestamp);
        }

        public static MessagePayload FromMessage(Message message)
        {
            return new MessagePayload()
            {
                name = message.Name,
                timestamp = message.TimeStamp.DateTime.ToUnixTime(),
                data = message.Data
            };
        }
    }
}