using System;
using Newtonsoft.Json;

namespace IO.Ably
{
    public enum PresenceAction : byte
    {
        Absent = 0,
        Present,
        Enter,
        Leave,
        Update
    }

    public class PresenceMessage : IMessage, IComparable<PresenceMessage>
    {
        public PresenceMessage()
        { }

        public PresenceMessage(PresenceAction action, string clientId)
            : this(action, clientId, null)
        { }

        public PresenceMessage(PresenceAction action, string clientId, object data)
        {
            Action = action;
            ClientId = clientId;
            Data = data;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("action")]
        public PresenceAction Action { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public string MemberKey => $"{ClientId}:{ConnectionId}";

        public int CompareTo(PresenceMessage other)
        {
            if (this.IsSynthesized() || other.IsSynthesized())
            {
                if (this.Timestamp > other.Timestamp) return -1;
                return this.Timestamp == other.Timestamp ? 0 : 1;
            }
            
            var thisValues = this.Id.Split(':');
            var otherValues = other.Id.Split(':');
            var msgSerialThis = int.Parse(thisValues[1]);
            var msgSerialOther = int.Parse(otherValues[1]);
            var indexThis = int.Parse(thisValues[2]);
            var indexOther = int.Parse(otherValues[2]);

            if (msgSerialThis == msgSerialOther)
            {
                if (indexThis > indexOther) return -1;
                return indexThis == indexOther ? 0 : 1;
            }
            
            if (msgSerialThis > msgSerialOther) return -1;
            return msgSerialThis == msgSerialOther ? 0 : 1;
        }
    }
}