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
            this.Action = action;
            this.ClientId = clientId;
            this.Data = data;
        }

        [JsonProperty(IdPropertyName)]
        [MessagePackMember(0, Name = IdPropertyName)]
        public string Id { get; set; }

        [JsonProperty(ActionPropertyName)]
        [MessagePackMember(1, Name = ActionPropertyName)]
        public ActionType Action { get; set; }

        [JsonProperty(ClientIdPropertyName)]
        [MessagePackMember(2, Name = ClientIdPropertyName)]
        public string ClientId { get; set; }

        [JsonProperty(ConnectionIdPropertyName)]
        [MessagePackMember(3, Name = ConnectionIdPropertyName)]
        public string ConnectionId { get; set; }

        [JsonProperty(DataPropertyName)]
        [MessagePackMember(4, Name = DataPropertyName)]
        public object Data { get; set; }

        [JsonProperty(EncodingPropertyName)]
        [MessagePackMember(5, Name = EncodingPropertyName)]
        public string Encoding { get; set; }

        [JsonProperty(TimestampPropertyName)]
        [MessagePackMember(6, Name = TimestampPropertyName)]
        public DateTimeOffset Timestamp { get; set; }
    }
}