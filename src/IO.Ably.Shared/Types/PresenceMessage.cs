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

    public class PresenceMessage : IMessage
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

        public PresenceMessage ShallowClone()
        {
            return (PresenceMessage)this.MemberwiseClone();
        }
    }
}