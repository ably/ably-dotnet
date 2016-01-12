using System;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    public class PresenceMessage : IEncodedMessage
    {
        internal const string IdPropertyName = "id";
        internal const string ActionPropertyName = "action";
        internal const string ClientIdPropertyName = "clientId";
        internal const string ConnectionIdPropertyName = "connectionId";
        internal const string DataPropertyName = "data";
        internal const string EncodingPropertyName = "encoding";
        internal const string TimestampPropertyName = "timestamp";

        public enum ActionType
        {
            Absent,
		    Present,
		    Enter,
		    Leave,
		    Update
        }

        public PresenceMessage()
        { }

        public PresenceMessage(ActionType action, string clientId)
            : this(action, clientId, null)
        { }

        public PresenceMessage(ActionType action, string clientId, object data)
        {
            this.action = action;
            this.clientId = clientId;
            this.data = data;
        }

        [MessagePackMember(0, Name = IdPropertyName)]
        public string id { get; set; }

        [MessagePackMember(1, Name = ActionPropertyName)]
        public ActionType action { get; set; }

        [MessagePackMember(2, Name = ClientIdPropertyName)]
        public string clientId { get; set; }

        [MessagePackMember(3, Name = ConnectionIdPropertyName)]
        public string connectionId { get; set; }

        [MessagePackMember(4, Name = DataPropertyName)]
        public object data { get; set; }

        [JsonProperty(EncodingPropertyName)]
        [MessagePackMember(5, Name = EncodingPropertyName)]
        public string encoding { get; set; }

        [JsonProperty(TimestampPropertyName)]
        [MessagePackMember(6, Name = TimestampPropertyName)]
        public DateTimeOffset timestamp { get; set; }
    }
}