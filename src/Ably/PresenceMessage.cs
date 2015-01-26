using System;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    public class PresenceMessage : IEncodedMessage
    {
        public enum ActionType
        {
            Absent,
		    Present,
		    Enter,
		    Leave,
		    Update
        }

        [JsonProperty("id")]
        [MessagePackMember(0, Name = "id")]
        public string Id { get; set; }

        [JsonProperty("action")]
        [MessagePackMember(1, Name = "action")]
        public ActionType Action { get; set; }

        [JsonProperty("clientId")]
        [MessagePackMember(2, Name = "clientId")]
        public string ClientId { get; set; }

        [JsonProperty("connectionId")]
        [MessagePackMember(3, Name = "connectionId")]
        public string ConnectionId { get; set; }

        [JsonProperty("data")]
        [MessagePackMember(4, Name = "data")]
        public object Data { get; set; }

        [JsonProperty("encoding")]
        [MessagePackMember(5, Name = "encoding")]
        public string Encoding { get; set; }

        [JsonProperty("timestamp")]
        [MessagePackMember(6, Name = "timestamp")]
        public DateTimeOffset TimeStamp { get; set; }
    }
}