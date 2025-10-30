using IO.Ably.Types;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json.Linq;

namespace IO.Ably.CustomSerialisers
{
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MessageExtrasMessagePackSerializer : MessagePackSerializer<MessageExtras>
    {
        public MessageExtrasMessagePackSerializer(SerializationContext ownerContext)
            : base(ownerContext)
        {
        }

        protected override void PackToCore(Packer packer, MessageExtras objectTree)
        {
            if (objectTree == null)
            {
                packer.PackNull();
                return;
            }

            var json = objectTree.ToJson();
            if (json == null)
            {
                packer.PackNull();
            }
            else
            {
                // Serialize as JSON string for compatibility
                packer.Pack(json.ToString(Newtonsoft.Json.Formatting.None));
            }
        }

        protected override MessageExtras UnpackFromCore(Unpacker unpacker)
        {
            if (unpacker.LastReadData.IsNil)
            {
                return null;
            }

            var jsonString = unpacker.LastReadData.AsString();
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            var jToken = JToken.Parse(jsonString);
            return MessageExtras.From(jToken);
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
