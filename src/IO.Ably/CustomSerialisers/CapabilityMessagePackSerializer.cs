using MsgPack;
using MsgPack.Serialization;

namespace IO.Ably.CustomSerialisers
{
    public class CapabilityMessagePackSerializer : MessagePackSerializer<Capability>
    {
        public CapabilityMessagePackSerializer( SerializationContext ownerContext ) : base( ownerContext ) { }

        protected override void PackToCore(Packer packer, Capability objectTree)
        {
            packer.Pack(objectTree.ToJson());
        }

        protected override Capability UnpackFromCore(Unpacker unpacker)
        {
            MessagePackObject obj = "";
            if(unpacker.ReadObject(out obj))
                return new Capability(obj.ToString());
            return new Capability();
        }
    }
}