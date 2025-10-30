using System;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>
    /// Presence Action: the event signified by a PresenceMessage.
    /// </summary>
    [MessagePackObject]
    public enum PresenceAction : byte
    {
        /// <summary>
        /// Absent.
        /// </summary>
        Absent = 0,

        /// <summary>
        /// Present.
        /// </summary>
        Present,

        /// <summary>
        /// Enter.
        /// </summary>
        Enter,

        /// <summary>
        /// Leave.
        /// </summary>
        Leave,

        /// <summary>
        /// Update.
        /// </summary>
        Update,
    }

    /// <summary>
    /// A class representing an individual presence update to be sent or received
    /// via the Ably Realtime service.
    /// </summary>
    [MessagePackObject]
    public class PresenceMessage : IMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceMessage"/> class.
        /// </summary>
        public PresenceMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceMessage"/> class.
        /// </summary>
        /// <param name="action">presence action.</param>
        /// <param name="clientId">id of client.</param>
        public PresenceMessage(PresenceAction action, string clientId)
            : this(action, clientId, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceMessage"/> class.
        /// </summary>
        /// <param name="action">presence action.</param>
        /// <param name="clientId">id of client.</param>
        /// <param name="data">custom data object passed with the presence message.</param>
        /// <param name="id">ably message id.</param>
        public PresenceMessage(PresenceAction action, string clientId, object data, string id = null)
        {
            Action = action;
            ClientId = clientId;
            Data = data;
            Id = id;
        }

        /// <summary>
        /// Ably message id.
        /// </summary>
        [Key(0)]
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Presence action associated with the presence message.
        /// </summary>
        [Key(1)]
        [JsonProperty("action")]
        public PresenceAction Action { get; set; }

        /// <summary>
        /// Id of the client associate.
        /// </summary>
        [Key(2)]
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Id of the current connection.
        /// </summary>
        [Key(3)]
        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        /// <summary>The connection key of the publisher of the message. Used for impersonation.</summary>
        [Key(4)]
        [JsonProperty("connectionKey")]
        public string ConnectionKey { get; set; }

        /// <summary>
        /// Custom data object associated with the message.
        /// </summary>
        [Key(5)]
        [JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        /// Encoding for the message.
        /// </summary>
        [Key(6)]
        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        /// <summary>
        /// Server timestamp for the message.
        /// </summary>
        [Key(7)]
        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Member key which is a combination of ClientId:ConnectionId.
        /// </summary>
        [IgnoreMember]
        [JsonIgnore]
        public string MemberKey => $"{ClientId}:{ConnectionId}";

        /// <summary>
        /// Clones the current object.
        /// </summary>
        /// <returns>a new Presence message.</returns>
        public PresenceMessage ShallowClone()
        {
            return (PresenceMessage)MemberwiseClone();
        }
    }
}
