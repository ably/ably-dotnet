using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace Ably.Types
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            JObject json = new JObject();
            json.Add("action", new JValue(message.Action));
            if (!string.IsNullOrEmpty(message.Channel))
            {
                json.Add("channel", new JValue(message.Channel));
            }
            json.Add("msgSerial", new JValue(message.MsgSerial));
            if (message.Messages != null)
            {
                JArray messagesArr = new JArray();

                foreach (Message m in message.Messages)
                {
                    JObject mJson = this.SerializeMessage(m);
                    if (mJson.HasValues)
                    {
                        messagesArr.Add(mJson);
                    }
                }

                if (messagesArr.HasValues)
                {
                    json.Add("messages", messagesArr);
                }
            }
            if (message.Presence != null)
            {
                JArray messagesArr = new JArray();

                foreach (PresenceMessage m in message.Presence)
                {
                    JObject mJson = this.SerializePresenceMessage(m);
                    if (mJson.HasValues)
                    {
                        messagesArr.Add(mJson);
                    }
                }

                if (messagesArr.HasValues)
                {
                    json.Add("presence", messagesArr);
                }
            }

            return json.ToString(Newtonsoft.Json.Formatting.None);
        }

        public ProtocolMessage DeserializeProtocolMessage(object value)
        {
            JObject json = JObject.Parse(value as string);
            ProtocolMessage message = new ProtocolMessage();

            JToken token;
            if (json.TryGetValue("action", out token))
            {
                message.Action = (ProtocolMessage.MessageAction)token.Value<int>();
            }
            if (json.TryGetValue("flags", out token))
            {
                message.Flags = (ProtocolMessage.MessageFlag)token.Value<int>();
            }
            if (json.TryGetValue("count", out token))
            {
                message.Count = token.Value<int>();
            }
            if (json.TryGetValue("msgSerial", out token))
            {
                message.MsgSerial = token.Value<long>();
            }
            if (json.TryGetValue("error", out token))
            {
                JObject errorJObject = token.Value<JObject>();
                string reason = errorJObject.OptValue<string>("message");
                int statusCode = errorJObject.OptValue<int>("statusCode");
                int code = errorJObject.OptValue<int>("code");
                message.Error = new ErrorInfo(reason, code, statusCode == 0 ? null : (System.Net.HttpStatusCode?)statusCode);
            }
            if (json.TryGetValue("id", out token))
            {
                message.Id = token.Value<string>();
            }
            if (json.TryGetValue("channel", out token))
            {
                message.Channel = token.Value<string>();
            }
            if (json.TryGetValue("channelSerial", out token))
            {
                message.ChannelSerial = token.Value<string>();
            }
            if (json.TryGetValue("connectionId", out token))
            {
                message.ConnectionId = token.Value<string>();
            }
            if (json.TryGetValue("connectionKey", out token))
            {
                message.ConnectionKey = token.Value<string>();
            }
            if (json.TryGetValue("connectionSerial", out token))
            {
                message.ConnectionSerial = token.Value<long>();
            }
            if (json.TryGetValue("timestamp", out token))
            {
                message.Timestamp = token.Value<long>().FromUnixTimeInMilliseconds();
            }
            if (json.TryGetValue("messages", out token))
            {
                JArray messagesArray = (JArray)token;
                message.Messages = messagesArray.Select(c => DeserializeMessage(c as JObject)).ToArray();
            }
            if (json.TryGetValue("presence", out token))
            {
                JArray messagesArray = (JArray)token;
                message.Presence = messagesArray.Select(c => DeserializePresenceMessage(c as JObject)).ToArray();
            }

            return message;
        }

        private JObject SerializeMessage(Message message)
        {
            JObject json = new JObject();
            if (!string.IsNullOrEmpty(message.Name))
            {
                json.Add("name", new JValue(message.Name));
            }
            if (message.Data != null)
            {
                if (message.Data is byte[])
                {
                    string encodedData = (message.Data as byte[]).ToBase64();
                    json.Add("data", new JValue(encodedData));
                    json.Add("encoding", new JValue("base64"));
                }
                else
                {
                    json.Add("data", new JValue(message.Data));
                }
            }
            return json;
        }

        private Message DeserializeMessage(JObject obj)
        {
            Message message = new Message();
            string encoding = "utf8";
            JToken token;
            if (obj.TryGetValue("name", out token))
            {
                message.Name = token.Value<string>();
            }
            if (obj.TryGetValue("timestamp", out token))
            {
                long timestamp = token.Value<long>();
                message.Timestamp = timestamp.FromUnixTimeInMilliseconds();
            }
            if (obj.TryGetValue("encoding", out token))
            {
                encoding = token.Value<string>();
            }
            if (obj.TryGetValue("data", out token))
            {
                message.Data = this.ParseToken(token, encoding);
            }

            return message;
        }

        private JObject SerializePresenceMessage(PresenceMessage message)
        {
            JObject json = new JObject();
            json.Add("action", new JValue(message.Action));
            if (!string.IsNullOrEmpty(message.Id))
            {
                json.Add("id", new JValue(message.Id));
            }
            if (!string.IsNullOrEmpty(message.ClientId))
            {
                json.Add("clientId", new JValue(message.ClientId));
            }
            if (!string.IsNullOrEmpty(message.ConnectionId))
            {
                json.Add("connectionId", new JValue(message.ConnectionId));
            }
            if (message.TimeStamp.Ticks > 0)
            {
                json.Add("timestamp", new JValue(message.TimeStamp.ToUnixTimeInMilliseconds()));
            }
            if (message.Data != null)
            {
                if (message.Data is byte[])
                {
                    string encodedData = (message.Data as byte[]).ToBase64();
                    json.Add("data", new JValue(encodedData));
                    json.Add("encoding", new JValue("base64"));
                }
                else
                {
                    json.Add("data", new JValue(message.Data));
                }
            }
            return json;
        }

        private PresenceMessage DeserializePresenceMessage(JObject obj)
        {
            PresenceMessage message = new PresenceMessage();
            string encoding = "utf8";
            JToken token;
            if (obj.TryGetValue("action", out token))
            {
                message.Action = (PresenceMessage.ActionType)token.Value<int>();
            }
            if (obj.TryGetValue("id", out token))
            {
                message.Id = token.Value<string>();
            }
            if (obj.TryGetValue("clientId", out token))
            {
                message.ClientId = token.Value<string>();
            }
            if (obj.TryGetValue("connectionId", out token))
            {
                message.ConnectionId = token.Value<string>();
            }
            if (obj.TryGetValue("timestamp", out token))
            {
                long timestamp = token.Value<long>();
                message.TimeStamp = timestamp.FromUnixTimeInMilliseconds();
            }
            if (obj.TryGetValue("encoding", out token))
            {
                encoding = token.Value<string>();
            }
            if (obj.TryGetValue("data", out token))
            {
                message.Data = this.ParseToken(token, encoding);
            }
            return message;
        }

        private object ParseToken(JToken token, string encoding)
        {
            switch (token.Type)
            {
                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Bytes:
                    return token.Value<byte[]>();
                case JTokenType.Date:
                    return token.Value<DateTime>();
                case JTokenType.Float:
                    return token.Value<float>();
                case JTokenType.Guid:
                    return token.Value<Guid>();
                case JTokenType.Integer:
                    return token.Value<int>();
                case JTokenType.String:
                    string value = token.Value<string>();
                    if (string.Equals(encoding, "BASE64", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return value.FromBase64();
                    }
                    else
                    {
                        return value;
                    }
                case JTokenType.TimeSpan:
                    return token.Value<TimeSpan>();
                case JTokenType.Uri:
                    return token.Value<Uri>();
                default:
                    return token;
            }
        }
    }
}
