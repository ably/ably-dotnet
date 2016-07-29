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
            this.clientId = clientId;
            this.data = data;
        }

        public string id { get; set; }

        public PresenceAction action { get; set; }

        public string clientId { get; set; }

        public string connectionId { get; set; }

        public object data { get; set; }

        public string encoding { get; set; }

        public DateTimeOffset? timestamp { get; set; }

        [JsonIgnore]
        public string MemberKey => $"{clientId}:{connectionId}";
    }
}