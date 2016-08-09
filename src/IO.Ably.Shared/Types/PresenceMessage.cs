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
            this.action = action;
            this.ClientId = clientId;
            this.Data = data;
        }

        public string Id { get; set; }

        public PresenceAction action { get; set; }

        public string ClientId { get; set; }

        public string connectionId { get; set; }

        public object Data { get; set; }

        public string Encoding { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public string MemberKey => $"{ClientId}:{connectionId}";
    }
}