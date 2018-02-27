using System;
using MsgPack;
using MsgPack.Serialization;

namespace IO.Ably.CustomSerialisers
{
    public class DateTimeOffsetMessagePackSerializer : MessagePackSerializer<DateTimeOffset>
    {
        public DateTimeOffsetMessagePackSerializer(SerializationContext ownerContext)
            : base(ownerContext) { }

        protected override void PackToCore(Packer packer, DateTimeOffset objectTree)
        {
            packer.Pack((long)objectTree.ToUnixTimeInMilliseconds());
        }

        protected override DateTimeOffset UnpackFromCore(Unpacker unpacker)
        {
            var data = unpacker.LastReadData;
            return data.AsInt64().FromUnixTimeInMilliseconds();
        }
    }
}
