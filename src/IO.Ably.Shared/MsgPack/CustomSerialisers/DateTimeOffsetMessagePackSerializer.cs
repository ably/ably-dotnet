using System;
using MessagePack;
using MessagePack.Formatters;

namespace IO.Ably.MsgPack.CustomSerialisers
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
            var nextType = reader.NextMessagePackType;

            // Handle integer types (Int64, Int32, etc.)
            if (nextType == MessagePackType.Integer)
            {
                var milliseconds = reader.ReadInt64();
                return milliseconds.FromUnixTimeInMilliseconds();
            }

            // Handle float types (Single, Double)
            if (nextType == MessagePackType.Float)
            {
                var milliseconds = reader.ReadDouble();
                return ((long)milliseconds).FromUnixTimeInMilliseconds();
            }

            // Handle string type (parse DateTimeOffset string representation)
            if (nextType == MessagePackType.String)
            {
                var value = reader.ReadString();
                if (DateTimeOffset.TryParse(value, out var result))
                {
                    return result;
                }
            }

            // Return MinValue if unable to parse
            return DateTimeOffset.MinValue;
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
