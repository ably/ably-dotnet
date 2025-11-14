using System;
using MessagePack;
using MessagePack.Formatters;

namespace IO.Ably.CustomSerialisers
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
    public class TimespanFormatter : IMessagePackFormatter<TimeSpan>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, TimeSpan value, MessagePackSerializerOptions options)
        {
            writer.Write((long)value.TotalMilliseconds);
        }

        /// <inheritdoc/>
        public TimeSpan Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var milliseconds = reader.ReadInt64();
            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
