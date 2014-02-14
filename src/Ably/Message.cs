using System.Collections.Generic;
using System.Linq;
using Ably.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Thrift.Protocol;
using TMessage = Ably.Protocol.TMessage;
using TType = Thrift.Protocol.TType;

namespace Ably
{
    public class Message
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public object Data { get; set; }
        public long Timestamp { get; set; }

        public bool IsBinaryMessage
        {
            get { return Data is byte[]; }
        }

        public T Value<T>()
        {
            object value = Value(typeof(T));
            if (value == null)
                return default(T);
            return (T)value;
        }

        public object Value(Type type)
        {
            if (IsBinaryMessage)
            {
                if (type == typeof(byte[]))
                    return (byte[])Data;
                throw new InvalidOperationException(
                    String.Format("Current message contains binary data which cannot be converted to {0}", type));
            }

            if (Data == null)
                return null;

            if (Data is JToken)
            {
                return ((JToken)Data).ToObject(type);
            }
            if (typeof(IConvertible).IsAssignableFrom(type))
            {
                return Convert.ChangeType(Data, type);
            }
            return Data;
        }

        public DateTimeOffset TimeStamp { get; set; }
    }

    public class MessagePayload
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("data")]
        public object Data { get; set; }
        [JsonProperty("encoding", NullValueHandling = NullValueHandling.Ignore)]
        public string Encoding { get; set; }
        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }
    }

    internal class MessageListJsonConverter : ObjectConverter<IEnumerable<Message>>
    {
        readonly MessageJsonConverter _messageConverter;
        public MessageListJsonConverter()
        {
            _messageConverter = new MessageJsonConverter();
        }

        public override string ToJsonString(IEnumerable<Message> value)
        {
            return string.Format("[{0}]", string.Join(",", value.Select(_messageConverter.ToJsonString)));
        }

        public override IEnumerable<Message> ToTarget(AblyResponse response)
        {
            return MessageJsonConverter.GetMessages(response.TextResponse);
        }
    }

    internal class MessageJsonConverter : ObjectConverter<Message>
    {
        public static object GetMessageData(JToken message)
        {
            var enconding = (string)message["encoding"];

            if (enconding.IsNotEmpty() && enconding == "base64")
            {
                return Convert.FromBase64String((string)message["data"]);
            }
            return (string)message["data"];
        }

        public override string ToJsonString(Message value)
        {
            var payload = new MessagePayload { Name = value.Name };
            payload.Timestamp = value.Timestamp;
            if (value.IsBinaryMessage)
            {
                payload.Data = Convert.ToBase64String((byte[])value.Data);
                payload.Encoding = "base64";
            }
            else
            {
                payload.Data = value.Data;
            }
            
            return JsonConvert.SerializeObject(value);
        }

        public override Message ToTarget(AblyResponse response)
        {
            return GetMessages(response.TextResponse).FirstOrDefault();
        }

        public static IEnumerable<Message> GetMessages(string jsonText)
        {
            var results = new List<Message>();
            var json = JArray.Parse(jsonText);
            foreach (var message in json)
            {
                results.Add(new Message
                {
                    Name = message.OptValue<string>("name"),
                    Data = GetMessageData(message),
                    TimeStamp = message.OptValue<long>("timestamp").FromUnixTimeInMilliseconds(),
                    ChannelId = message.OptValue<string>("client_id")
                });
            }
            return results;
        }
    }

    internal class PartialMessageJsonConverter : ObjectConverter<IPartialResult<Message>>
    {
        public override string ToJsonString(IPartialResult<Message> value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public override IPartialResult<Message> ToTarget(AblyResponse response)
        {
            var results = new PartialResult<Message>(100);
            if (response.TextResponse.IsEmpty())
                return results;

            results.AddRange(MessageJsonConverter.GetMessages(response.TextResponse));
            results.NextQuery = DataRequestQuery.GetLinkQuery(response.Headers, "next");
            results.CurrentResultQuery = DataRequestQuery.GetLinkQuery(response.Headers, "current");
            results.InitialResultQuery = DataRequestQuery.GetLinkQuery(response.Headers, "first");
            return results;
        }
    }

    internal abstract class ObjectConverter<TTarget> : IObjectConverter
    {
        public bool CanHandleType(Type type)
        {
            return typeof(TTarget).IsAssignableFrom(type);
        }
        public abstract string ToJsonString(TTarget value);
        public abstract TTarget ToTarget(AblyResponse response);

        string IObjectConverter.ToJsonString(object value)
        {
            return ToJsonString((TTarget)value);
        }

        object IObjectConverter.ToTarget(AblyResponse response)
        {
            return ToTarget(response);
        }
    }

    internal interface IObjectConverter
    {
        bool CanHandleType(Type type);
        string ToJsonString(object value);
        object ToTarget(AblyResponse response);
    }
}
