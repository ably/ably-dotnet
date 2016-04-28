using System;
using System.IO;
using IO.Ably.CustomSerialisers;
using MsgPack;
using MsgPack.Serialization;

namespace IO.Ably
{
    internal class MsgPackHelper
    {
        public static SerializationContext GetContext()
        {
            var context = new SerializationContext() { SerializationMethod = SerializationMethod.Map};
            context.Serializers.Register(new CapabilityMessagePackSerializer(context));
            context.Serializers.Register(new DateTimeOffsetMessagePackSerializer(context));
            return context;
        }

        public static byte[] Serialise(object obj)
        {
            var serialiser = GetContext().GetSerializer(obj.GetType());
            using (var ms = new MemoryStream())
            {
                serialiser.Pack(ms, obj, PackerCompatibilityOptions.None);
                return ms.ToArray();
            }
        }

        public static object DeSerialise(byte[] byteArray, Type objectType)
        {
            if (byteArray == null || byteArray.Length == 0)
                return null;

            using (var ms = new MemoryStream(byteArray))
            {
                var serialiser = GetContext().GetSerializer(objectType);
                return serialiser.Unpack(ms);
            }
        }
    }
}