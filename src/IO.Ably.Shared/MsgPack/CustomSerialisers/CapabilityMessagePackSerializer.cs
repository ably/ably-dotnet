using MessagePack;
using MessagePack.Formatters;

namespace IO.Ably.MsgPack.CustomSerialisers
{
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class CapabilityFormatter : IMessagePackFormatter<Capability>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, Capability value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.Write(value.ToJson());
        }

        /// <inheritdoc/>
        public Capability Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return new Capability();
            }

            var json = reader.ReadString();
            return new Capability(json);
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
