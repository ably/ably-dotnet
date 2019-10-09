using System;
using MsgPack;
using MsgPack.Serialization;

namespace IO.Ably.CustomSerialisers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internal serializers")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "CS1591:Missing XML comment for publicly visible type or member", Justification = "Internal serializers")]
    public class TimespanMessagePackSerializer : MessagePackSerializer<TimeSpan>
    {
        public TimespanMessagePackSerializer(SerializationContext ownerContext)
            : base(ownerContext) { }

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
