using System;
using System.Collections.Generic;
using System.Text;
using IO.Ably.CustomSerialisers;
using IO.Ably.Push;
using IO.Ably.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MessagePack;
using MessagePack.Resolvers;
using IO.Ably.MsgPack.CustomSerialisers;

namespace IO.Ably
{
    internal static class MsgPackHelper
    {
        // Use AOT-safe resolvers for Unity compatibility
        // StandardResolver uses dynamic code generation which fails in IL2CPP/Mono
        // We create options from scratch without .Standard to avoid any dynamic compilation
        internal static IFormatterResolver[] Resolvers =
        {
                AblyGeneratedResolver.Instance,       // 1. Check generated code first (Fastest/Specific)
                AblyResolver.Instance,                // 2. Check manual custom resolvers
                BuiltinResolver.Instance,             // 3. Check standard types (List, DateTime)
                DynamicGenericResolver.Instance,      // Priority 4: Collections, arrays, tuples
                PrimitiveObjectResolver.Instance,     // Priority 5: typeof(object) fields
        };

        private static readonly MessagePackSerializerOptions DefaultOptions = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(Resolvers))
            .WithSecurity(MessagePackSecurity.UntrustedData);

        public static byte[] Serialise<T>(T obj, MessagePackSerializerOptions options = null)
        {
            if (options == null)
            {
                options = DefaultOptions;
            }

            if (obj == null)
            {
                return null;
            }

            // Handle JToken types (JObject, JArray, etc.)
            if (obj is JToken jToken)
            {
                return MessagePackSerializer.ConvertFromJson(jToken.ToString());
            }

            return MessagePackSerializer.Serialize<T>(obj, options);
        }

        public static T Deserialise<T>(byte[] byteArray, MessagePackSerializerOptions options = null)
        {
            if (options == null)
            {
                options = DefaultOptions;
            }

            if (byteArray == null || byteArray.Length == 0)
            {
                return default(T);
            }

            // Checks if T is JToken or derives from JToken (JObject, JArray, etc.)
            if (typeof(JToken).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)JToken.Parse(ToJsonString(byteArray));
            }

            return MessagePackSerializer.Deserialize<T>(byteArray, options);
        }

        /// <summary>
        /// Serialise method for object type (used when PostData is passed as object).
        /// This method explicitly handles all known types that can be assigned to PostData.
        /// </summary>
        public static byte[] SerialiseObject(object obj, MessagePackSerializerOptions options = null)
        {
            if (options == null)
            {
                options = DefaultOptions;
            }

            if (obj == null)
            {
                return null;
            }

            // Handle specific known types that are assigned to PostData
            if (obj is IEnumerable<Message> messages)
            {
                return Serialise(messages, options);
            }

            if (obj is DeviceDetails deviceDetails)
            {
                return Serialise(deviceDetails, options);
            }

            if (obj is PushChannelSubscription subscription)
            {
                return Serialise(subscription, options);
            }

            if (obj is TokenRequest tokenRequest)
            {
                return Serialise(tokenRequest, options);
            }

            if (obj is ProtocolMessage protocolMessage)
            {
                return Serialise(protocolMessage, options);
            }

            // Fallback to dynamic serialization for any other types
            // This preserves the runtime type information
            return Serialise<object>(obj, options);
        }

        // This uses MessagePack's built-in JSON conversion which handles all MessagePack types
        public static string ToJsonString(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null;
            }

            return MessagePackSerializer.ConvertToJson(byteArray, DefaultOptions);
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
