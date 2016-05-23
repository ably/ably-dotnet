﻿using System;
using MsgPack.Serialization;

namespace IO.Ably
{
    public class PresenceMessage : IMessage
    {
        public enum ActionType : byte
        {
            Absent = 0,
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

        [MessagePackMember(0)]
        public string id { get; set; }

        [MessagePackMember(1)]
        public ActionType action { get; set; }

        [MessagePackMember(2)]
        public string clientId { get; set; }

        [MessagePackMember(3)]
        public string connectionId { get; set; }

        [MessagePackMember(4)]
        public object data { get; set; }

        [MessagePackMember(5)]
        public string encoding { get; set; }

        [MessagePackMember(6)]
        public DateTimeOffset? timestamp { get; set; }
    }
}