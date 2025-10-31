using System;
using MessagePack;
using MessagePack.Formatters;

namespace IO.Ably.CustomSerialisers
{
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class DateTimeOffsetFormatter : IMessagePackFormatter<DateTimeOffset>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, MessagePackSerializerOptions options)
        {
            writer.Write(value.ToUnixTimeInMilliseconds());
        }

        /// <inheritdoc/>
        public DateTimeOffset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var milliseconds = reader.ReadInt64();
            return milliseconds.FromUnixTimeInMilliseconds();
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
