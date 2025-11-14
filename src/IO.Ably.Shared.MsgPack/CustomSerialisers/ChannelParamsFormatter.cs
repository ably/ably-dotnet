using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

namespace IO.Ably.CustomSerialisers
{
    /// <summary>
    /// Custom formatter for ChannelParams that serializes it as a dictionary.
    /// </summary>
    public class ChannelParamsFormatter : IMessagePackFormatter<ChannelParams>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, ChannelParams value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(value.Count);
            foreach (var kvp in value)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        /// <inheritdoc/>
        public ChannelParams Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var count = reader.ReadMapHeader();
            var result = new ChannelParams();

            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                result[key] = value;
            }

            return result;
        }
    }
}
