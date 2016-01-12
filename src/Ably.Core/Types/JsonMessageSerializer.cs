using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace Ably.Types
{
    // TODO: drop this whole class, instead use Newtonsoft.Json attributes like [JsonObject], [JsonProperty]
    public class JsonMessageSerializer : IMessageSerializer
    {
        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            JObject json = new JObject();
            json.Add(ProtocolMessage.ActionPropertyName, new JValue(message.action));
            if (!string.IsNullOrEmpty(message.channel))
            {
                json.Add(ProtocolMessage.ChannelPropertyName, new JValue(message.channel));
            }
            json.Add(ProtocolMessage.MsgSerialPropertyName, new JValue(message.msgSerial));
            if (message.messages != null)
            {
                JArray messagesArr = new JArray();

                foreach (Message m in message.messages)
                {
                    JObject mJson = this.SerializeMessage(m);
                    if (mJson.HasValues)
                    {
                        messagesArr.Add(mJson);
                    }
                }

                if (messagesArr.HasValues)
                {
                    json.Add(ProtocolMessage.MessagesPropertyName, messagesArr);
                }
            }
            if (message.presence != null)
            {
                JArray messagesArr = new JArray();

                foreach (PresenceMessage m in message.presence)
                {
                    JObject mJson = this.SerializePresenceMessage(m);
                    if (mJson.HasValues)
                    {
                        messagesArr.Add(mJson);
                    }
                }

                if (messagesArr.HasValues)
                {
                    json.Add(ProtocolMessage.PresencePropertyName, messagesArr);
                }
            }

            return json.ToString(Newtonsoft.Json.Formatting.None);
        }

        public ProtocolMessage DeserializeProtocolMessage(object value)
        {
            JObject json = JObject.Parse(value as string);
            ProtocolMessage message = new ProtocolMessage();

            JToken token;
            if (json.TryGetValue(ProtocolMessage.ActionPropertyName, out token))
            {
                message.action = (ProtocolMessage.MessageAction)token.Value<int>();
            }
            if (json.TryGetValue(ProtocolMessage.FlagsPropertyName, out token))
            {
                message.flags = (ProtocolMessage.MessageFlag)token.Value<int>();
            }
            if (json.TryGetValue(ProtocolMessage.CountPropertyName, out token))
            {
                message.count = token.Value<int>();
            }
            if (json.TryGetValue(ProtocolMessage.MsgSerialPropertyName, out token))
            {
                message.msgSerial = token.Value<long>();
            }
            if (json.TryGetValue(ProtocolMessage.ErrorPropertyName, out token))
            {
                JObject errorJObject = token.Value<JObject>();
                string reason = errorJObject.OptValue<string>(ErrorInfo.ReasonPropertyName);
                int statusCode = errorJObject.OptValue<int>(ErrorInfo.StatusCodePropertyName);
                int code = errorJObject.OptValue<int>(ErrorInfo.CodePropertyName);
                message.error = new ErrorInfo(reason, code, statusCode == 0 ? null : (System.Net.HttpStatusCode?)statusCode);
            }
            if (json.TryGetValue(ProtocolMessage.IdPropertyName, out token))
            {
                message.id = token.Value<string>();
            }
            if (json.TryGetValue(ProtocolMessage.ChannelPropertyName, out token))
            {
                message.channel = token.Value<string>();
            }
            if (json.TryGetValue(ProtocolMessage.ChannelSerialPropertyName, out token))
            {
                message.channelSerial = token.Value<string>();
            }
            if (json.TryGetValue(ProtocolMessage.ConnectionIdPropertyName, out token))
            {
                message.connectionId = token.Value<string>();
            }
            if (json.TryGetValue(ProtocolMessage.ConnectionKeyPropertyName, out token))
            {
                message.connectionKey = token.Value<string>();
            }
            if (json.TryGetValue(ProtocolMessage.ConnectionSerialPropertyName, out token))
            {
                message.connectionSerial = token.Value<long>();
            }
            if (json.TryGetValue(ProtocolMessage.TimestampPropertyName, out token))
            {
                message.timestamp = token.Value<long>().FromUnixTimeInMilliseconds();
            }
            if (json.TryGetValue(ProtocolMessage.MessagesPropertyName, out token))
            {
                JArray messagesArray = (JArray)token;
                message.messages = messagesArray.Select(c => DeserializeMessage(c as JObject)).ToArray();
            }
            if (json.TryGetValue(ProtocolMessage.PresencePropertyName, out token))
            {
                JArray messagesArray = (JArray)token;
                message.presence = messagesArray.Select(c => DeserializePresenceMessage(c as JObject)).ToArray();
            }

            return message;
        }

        private JObject SerializeMessage(Message message)
        {
            JObject json = new JObject();
            if (!string.IsNullOrEmpty(message.Name))
            {
                json.Add(Message.NamePropertyName, new JValue(message.Name));
            }
            if (message.Data != null)
            {
                if (message.Data is byte[])
                {
                    string encodedData = (message.Data as byte[]).ToBase64();
                    json.Add(Message.DataPropertyName, new JValue(encodedData));
                    json.Add(Message.EncodingPropertyName, new JValue("base64"));
                }
                else
                {
                    json.Add(Message.DataPropertyName, new JValue(message.Data));
                }
            }
            return json;
        }

        private Message DeserializeMessage(JObject obj)
        {
            Message message = new Message();
            string encoding = "utf8";
            JToken token;
            if (obj.TryGetValue(Message.NamePropertyName, out token))
            {
                message.Name = token.Value<string>();
            }
            if (obj.TryGetValue(Message.TimestampPropertyName, out token))
            {
                long timestamp = token.Value<long>();
                message.Timestamp = timestamp.FromUnixTimeInMilliseconds();
            }
            if (obj.TryGetValue(Message.EncodingPropertyName, out token))
            {
                encoding = token.Value<string>();
            }
            if (obj.TryGetValue(Message.DataPropertyName, out token))
            {
                message.Data = this.ParseToken(token, encoding);
            }

            return message;
        }

        private JObject SerializePresenceMessage(PresenceMessage message)
        {
            JObject json = new JObject();
            json.Add(PresenceMessage.ActionPropertyName, new JValue(message.Action));
            if (!string.IsNullOrEmpty(message.Id))
            {
                json.Add(Message.IdPropertyName, new JValue(message.Id));
            }
            if (!string.IsNullOrEmpty(message.ClientId))
            {
                json.Add(Message.ClientIdPropertyName, new JValue(message.ClientId));
            }
            if (!string.IsNullOrEmpty(message.ConnectionId))
            {
                json.Add(Message.ConnectionIdPropertyName, new JValue(message.ConnectionId));
            }
            if (message.Timestamp.Ticks > 0)
            {
                json.Add(Message.TimestampPropertyName, new JValue(message.Timestamp.ToUnixTimeInMilliseconds()));
            }
            if (message.Data != null)
            {
                if (message.Data is byte[])
                {
                    string encodedData = (message.Data as byte[]).ToBase64();
                    json.Add(Message.DataPropertyName, new JValue(encodedData));
                    json.Add(Message.EncodingPropertyName, new JValue("base64"));
                }
                else
                {
                    json.Add(Message.DataPropertyName, new JValue(message.Data));
                }
            }
            return json;
        }

        private PresenceMessage DeserializePresenceMessage(JObject obj)
        {
            PresenceMessage message = new PresenceMessage();
            string encoding = "utf8";
            JToken token;
            if (obj.TryGetValue(PresenceMessage.ActionPropertyName, out token))
            {
                message.Action = (PresenceMessage.ActionType)token.Value<int>();
            }
            if (obj.TryGetValue(PresenceMessage.IdPropertyName, out token))
            {
                message.Id = token.Value<string>();
            }
            if (obj.TryGetValue(PresenceMessage.ClientIdPropertyName, out token))
            {
                message.ClientId = token.Value<string>();
            }
            if (obj.TryGetValue(PresenceMessage.ConnectionIdPropertyName, out token))
            {
                message.ConnectionId = token.Value<string>();
            }
            if (obj.TryGetValue(PresenceMessage.TimestampPropertyName, out token))
            {
                long timestamp = token.Value<long>();
                message.Timestamp = timestamp.FromUnixTimeInMilliseconds();
            }
            if (obj.TryGetValue(PresenceMessage.EncodingPropertyName, out token))
            {
                encoding = token.Value<string>();
            }
            if (obj.TryGetValue(PresenceMessage.DataPropertyName, out token))
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
                    if (string.Equals(encoding, "BASE64", StringComparison.OrdinalIgnoreCase))
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
