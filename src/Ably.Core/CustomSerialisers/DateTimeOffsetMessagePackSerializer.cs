using System;
using MsgPack;
using MsgPack.Serialization;

namespace IO.Ably.CustomSerialisers
{
    public class DateTimeOffsetMessagePackSerializer : MessagePackSerializer<DateTime>
    {
        public DateTimeOffsetMessagePackSerializer( SerializationContext ownerContext ) : base( ownerContext ) { }

        protected override void PackToCore(Packer packer, DateTime objectTree)
        {
            packer.Pack(objectTree.ToUnixTimeInMilliseconds());
        }

        protected override DateTime UnpackFromCore(Unpacker unpacker)
        {
            var data = unpacker.LastReadData;
            return data.AsInt64().FromUnixTimeInMilliseconds();
        }
    }
}