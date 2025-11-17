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
            var nextType = reader.NextMessagePackType;

            // Handle integer types (Int64, Int32, etc.)
            if (nextType == MessagePackType.Integer)
            {
                var milliseconds = reader.ReadInt64();
                return TimeSpan.FromMilliseconds(milliseconds);
            }

            // Handle float types (Single, Double)
            if (nextType == MessagePackType.Float)
            {
                var milliseconds = reader.ReadDouble();
                return TimeSpan.FromMilliseconds(milliseconds);
            }

            // Handle string type (parse TimeSpan string representation)
            if (nextType == MessagePackType.String)
            {
                var value = reader.ReadString();
                if (TimeSpan.TryParse(value, out var result))
                {
                    return result;
                }
            }

            // Return null TimeSpan if unable to parse
            return TimeSpan.MinValue;
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
