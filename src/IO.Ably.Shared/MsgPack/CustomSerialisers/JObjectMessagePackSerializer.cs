using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json.Linq;

namespace IO.Ably.MsgPack.CustomSerialisers
{
    internal class JObjectMessagePackSerializer : IMessagePackFormatter<JObject>
    {
        void IMessagePackFormatter<JObject>.Serialize(ref MessagePackWriter writer, JObject value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var bytes = MessagePackSerializer.ConvertFromJson(value.ToJson());
            writer.WriteRaw(bytes);
        }

        JObject IMessagePackFormatter<JObject>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // Check if there's any data left to read from the current position
            var remainingSequence = reader.Sequence.Slice(reader.Position);
            if (remainingSequence.Length == 0)
            {
                return null;
            }

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

            return JObject.Parse(jsonString);
        }
    }
}
