using System;
using System.Text;
using IO.Ably.CustomSerialisers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                    AblyGeneratedResolver.Instance,
                    StandardResolver.Instance));

        public static byte[] Serialise(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            if (obj is JToken value)
            {
                return MessagePackSerializer.ConvertFromJson(value.ToString());
            }

            return MessagePackSerializer.Serialize(obj.GetType(), obj, Options);
        }

        public static object Deserialise(byte[] byteArray, Type objectType)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            // Checks if given type is subset of JToken
            if (typeof(JToken).IsAssignableFrom(objectType))
            {
                return JToken.Parse(ToJsonString(byteArray));
            }

            return MessagePackSerializer.Deserialize(objectType, byteArray, Options);
        }

        public static T Deserialise<T>(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return default(T);
            }

            return MessagePackSerializer.Deserialize<T>(byteArray, Options);
        }

        // This uses MessagePack's built-in JSON conversion which handles all MessagePack types
        public static string ToJsonString(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            return MessagePackSerializer.ConvertToJson(byteArray, Options);
        }

        public static string DecodeMsgPackObject(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                // Convert MessagePack binary data to JSON string for pretty printing
                return JToken.Parse(ToJsonString(byteArray)).ToString(Formatting.Indented);
            }
            catch (Exception)
            {
                // Last resort: return hex representation for debugging
                var sb = new StringBuilder(byteArray.Length * 2);
                sb.Append("0x");
                foreach (byte b in byteArray)
                {
                    sb.AppendFormat("{0:x2}", b);
                }

                return sb.ToString();
            }
        }
    }
}
