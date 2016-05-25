using System;
using MsgPack;
using MsgPack.Serialization;

namespace IO.Ably.CustomSerialisers
{
    public class TimespanMessagePackSerializer : MessagePackSerializer<TimeSpan>
    {
        public TimespanMessagePackSerializer(SerializationContext ownerContext) : base(ownerContext) { }

        protected override void PackToCore(Packer packer, TimeSpan objectTree)
        {
            packer.Pack((long)objectTree.TotalMilliseconds);
        }

        protected override TimeSpan UnpackFromCore(Unpacker unpacker)
        {
            var data = unpacker.LastReadData;
            return TimeSpan.FromMilliseconds(data.AsInt64());
        }
    }
}