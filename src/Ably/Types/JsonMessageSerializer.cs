using System.Linq;
using Newtonsoft.Json.Linq;
using System;

namespace Ably.Types
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            JObject json = new JObject();
            json.Add("action", new JValue(message.Action));
            json.Add("channel", new JValue(message.Channel));
            if (message.Messages != null)
            {
                JArray messagesArr = new JArray();

                foreach (Message m in message.Messages)
                {
                    messagesArr.Add(this.SerializeMessage(m));
                }

                json.Add("messages", messagesArr);
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
                // TODO: Implement
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
                message.Timestamp = token.Value<long>();
            }
            if (json.TryGetValue("messages", out token))
            {
                JArray messagesArray = (JArray)token;
                message.Messages = messagesArray.Select(c => DeserializeMessage(c as JObject)).ToArray();
            }
            if (json.TryGetValue("presence", out token))
            {
                // TODO: Implement
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
            return json;
        }

        private Message DeserializeMessage(JObject obj)
        {
            Message message = new Message();

            JToken token;
            if (obj.TryGetValue("name", out token))
            {
                message.Name = token.Value<string>();
            }
            if (obj.TryGetValue("timestamp", out token))
            {
                //message.TimeStamp = token.Value<long>().FromUnixTime();
            }

            return message;
        }
    }
}
