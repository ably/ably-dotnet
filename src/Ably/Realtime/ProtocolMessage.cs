using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ably.Protocol;
using Newtonsoft.Json.Linq;

namespace Ably.Realtime
{
    public class ProtocolMessage
    {
        public static ProtocolMessage fromJSON(JToken json)
        {
            ProtocolMessage result = new ProtocolMessage();
            //if(json != null) {
            //    result.action = TAction.findByValue(json.optInt("action"));
            //    result.count = json.optInt("count");
            //    if(json.has("error"))
            //        result.error = new ErrorInfo(json.optJSONObject("error"));
            //    if(json.has("channel"))
            //        result.channel = json.optString("channel");
            //    if(json.has("channelSerial"))
            //        result.channelSerial = json.optString("channelSerial");
            //    if(json.has("connectionId"))
            //        result.connectionId = json.optString("connectionId");
            //    result.connectionSerial = json.optLong("connectionSerial");
            //    result.msgSerial = json.optLong("msgSerial");
            //    result.timestamp = json.optLong("timestamp");
            //    if(json.has("messages"))
            //        result.messages = Message.fromJSON(json.optJSONArray("messages"));
            //    if(json.has("presence"))
            //        result.presence = PresenceMessage.fromJSON(json.optJSONArray("presence"));
            //}
            return result;
        }

        public static ProtocolMessage fromThrift(TProtocolMessage thrift)
        {
            var result = new ProtocolMessage();
            //result.Action = thrift.getAction();
            //result.Count = thrift.getCount();
            //TError thriftErr = thrift.getError(); if(thriftErr != null) result.error = new ErrorInfo(thriftErr);
            //result.channel = thrift.getChannel();
            //result.channelSerial = thrift.getChannelSerial();
            //result.connectionId = thrift.getConnectionId();
            //result.connectionSerial = thrift.getConnectionSerial();
            //result.msgSerial = thrift.getMsgSerial();
            //result.timestamp = thrift.getTimestamp();
            //result.messages = Message.fromThrift(thrift.getMessages());
            //result.presence = PresenceMessage.fromThrift(thrift.getPresence());

            return result;
        }

        private JObject toJSON()
        {
            JObject json = new JObject();
            //try {
            //    json["action"] = Action;
            //    json["channel"] = Channel;
            //    json["msgSerial"] = MsgSerial;
            //    if(Messages.Count > 0 ) json["messages"] = Message.asJSON(messages);
            //    if(presence != null) json.put("presence", PresenceMessage.asJSON(presence));

            //    return json;
            //} catch(JSONException e) {
            //    throw new AblyException("Unexpected exception encoding message; err = " + e, 400, 40000);
            //}
            return json;
        }

        public JObject asJSON(ProtocolMessage message)
        {
            return message.toJSON();
        }

        public JArray asJSON(ProtocolMessage[] messages)
        {
            JArray json;
            try
            {
                json = new JArray(messages.Length);
                for (int i = 0; i < messages.Length; i++)
                    json.Add(messages[i].toJSON());

                return json;
            }
            catch (Exception e)
            {
                throw new AblyException(e);
            }
        }

        public TProtocolMessage asThrift(ProtocolMessage message)
        {
            TProtocolMessage tMessage = new TProtocolMessage();
            //tMessage.setAction(message.action);
            //tMessage.setChannel(message.channel);
            //tMessage.setMsgSerial(message.msgSerial);
            //if(message.messages != null) tMessage.setMessages(Message.asThrift(message.messages));
            //if(message.presence != null) tMessage.SetPresence(PresenceMessage.asThrift(message.presence));
            return tMessage;
        }

        public List<TProtocolMessage> asThrift(ProtocolMessage[] messages)
        {
            List<TProtocolMessage> result = new List<TProtocolMessage>();
            foreach (ProtocolMessage message in messages)
            {
                result.Add(asThrift(message));
            }
            return result;
        }

        public static bool mergeTo(ProtocolMessage dest, ProtocolMessage src)
        {
            if (dest.Channel == src.Channel)
            {
                if (dest.Action == src.Action)
                {
                    switch (dest.Action)
                    {
                        case TAction.MESSAGE:
                            {
                                dest.Messages.AddRange(src.Messages);
                                return true;
                            }
                        case TAction.PRESENCE:
                            {
                                dest.Presence.AddRange(src.Presence);
                                return true;
                            }
                    }
                }
            }
            return false;
        }

        public static bool AckRequired(ProtocolMessage msg)
        {
            return (msg.Action == TAction.MESSAGE || msg.Action == TAction.PRESENCE);
        }

        public ProtocolMessage()
        {
        }

        public ProtocolMessage(TAction action)
        {
            Action = action;
        }

        public ProtocolMessage(TAction action, String channel)
        {
            Action = action;
            Channel = channel;
        }

        public TAction Action { get; set; }
        public int Count { get; set; }
        public ErrorInfo Error { get; set; }
        public String Channel { get; set; }
        public String ChannelSerial { get; set; }
        public String ConnectionId { get; set; }
        public long ConnectionSerial { get; set; }
        public long MsgSerial { get; set; }
        public long Timestamp { get; set; }
        public List<Message> Messages { get; set; }
        public List<PresenceMessage> Presence { get; set; }

    }
}
