using System;
using System.Collections.Generic;
using MsgPack;

namespace IO.Ably.Types.MsgPack
{
    internal static class MsgPackUtils
    {
        private static readonly Dictionary<Type, Func<MessagePackObject, object>> resolver = new Dictionary
            <Type, Func<MessagePackObject, object>>
        {
            {typeof (byte), r => r.AsByte()},
            {typeof (sbyte), r => r.AsSByte()},
            {typeof (bool), r => r.AsBoolean()},
            {typeof (ushort), r => r.AsUInt16()},
            {typeof (uint), r => r.AsUInt32()},
            {typeof (ulong), r => r.AsUInt64()},
            {typeof (short), r => r.AsInt16()},
            {typeof (int), r => r.AsInt32()},
            {typeof (long), r => r.AsInt64()},
            {typeof (float), r => r.AsSingle()},
            {typeof (double), r => r.AsDouble()},
            {typeof (string), r => r.AsStringUtf8()}
        };

        public static object unpack(this MessagePackObject mp)
        {
            return ParseResult(mp);
        }

        private static object ParseResult(MessagePackObject obj)
        {
            if (obj.IsList)
            {
                var data = new List<object>();
                foreach (var objItem in obj.AsList())
                {
                    data.Add(ParseResult(objItem));
                }
                return data.ToArray();
            }
            if (obj.IsMap)
            {
                var data = new Dictionary<object, object>();
                foreach (var objItem in obj.AsDictionary())
                {
                    data.Add(ParseResult(objItem.Key), ParseResult(objItem.Value));
                }
                return data;
            }
            if (obj.UnderlyingType != null && resolver.ContainsKey(obj.UnderlyingType))
            {
                return resolver[obj.UnderlyingType](obj);
            }
            return null;
        }

        public static void packArray<tElt>(this Packer packer, TypeMetadata mdMessage, tElt[] arr)
        {
            packer.PackArrayHeader(arr.Length);
            foreach (var m in arr)
                mdMessage.serialize(m, packer);
        }

        public static tElt[] unpackArray<tElt>(this Unpacker unpacker, TypeMetadata mdMessage)
        {
            long ll;
            unpacker.ReadArrayLength(out ll);
            var l = (int) ll;
            var arr = new tElt[l];
            for (var i = 0; i < l; i++)
            {
                arr[i] = (tElt) mdMessage.deserialize(unpacker);
            }
            return arr;
        }
    }
}