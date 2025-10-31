using System;
using IO.Ably.CustomSerialisers;
using MessagePack;
using MessagePack.Resolvers;

namespace IO.Ably
{
    internal static class MsgPackHelper
    {
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(
                    AblyResolver.Instance,
                    StandardResolver.Instance));

        public static byte[] Serialise(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            return MessagePackSerializer.Serialize(obj.GetType(), obj, Options);
        }

        public static object Deserialise(byte[] byteArray, Type objectType)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            return MessagePackSerializer.Deserialize(objectType, byteArray, Options);
        }

        public static object DeserialiseMsgPackObject(byte[] byteArray)
        {
            // MessagePackObject doesn't exist in MessagePack-CSharp
            // Return as dynamic object instead
            return Deserialise(byteArray, typeof(object));
        }

        public static T Deserialise<T>(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return default(T);
            }

            return MessagePackSerializer.Deserialize<T>(byteArray, Options);
        }
    }
}
