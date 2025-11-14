using IO.Ably.Types;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MessageExtrasFormatter : IMessagePackFormatter<MessageExtras>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, MessageExtras value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var jToken = value.ToJson();
            if (jToken == null)
            {
                writer.WriteNil();
            }
            else
            {
                var bytes = MessagePackSerializer.ConvertFromJson(jToken.ToString());
                writer.WriteRaw(bytes);
            }
        }

        /// <inheritdoc/>
        public MessageExtras Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var bytes = reader.ReadRaw();
            if (bytes.Length == 0)
            {
                return null;
            }

            var jsonString = MessagePackSerializer.ConvertToJson(bytes);
            if (jsonString.IsEmpty())
            {
                return null;
            }

            var jToken = JToken.Parse(jsonString);
            return MessageExtras.From(jToken);
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
