using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MsgPack;
using Newtonsoft.Json;

namespace IO.Ably.Types.MsgPack
{
    internal class TypeMetadata
    {
        private readonly Dictionary<string, PropertyMetadata> dict = new Dictionary<string, PropertyMetadata>();
        private readonly List<PropertyMetadata> list = new List<PropertyMetadata>();

        public readonly Type type;

        public TypeMetadata(Type tp)
        {
            type = tp;
            var props = new List<PropertyMetadata>();
            // Enum the properties
            foreach (var pi in tp.GetRuntimeProperties())
            {
                if (pi.IsSpecialName)
                    continue;
                var m = pi.GetMethod ?? pi.SetMethod;
                if (m.IsStatic)
                    continue;
                if (!m.IsPublic)
                    continue;

                if (null != pi.GetCustomAttribute<JsonIgnoreAttribute>())
                    continue;

                var pm = new PropertyMetadata(pi);
                list.Add(pm);
                dict.Add(pm.name, pm);
            }
        }

        public void remove(string n)
        {
            var pm = dict[n];
            dict.Remove(n);
            list.Remove(pm);
        }

        public void add(string clrPropName, string desializeName = null)
        {
            var pi = type.GetRuntimeProperty(clrPropName);
            var pm = new PropertyMetadata(pi, desializeName);
            list.Add(pm);
            dict.Add(pm.name, pm);
        }

        public void setCustom(string name, Action<object, Packer> serialize, Action<Unpacker, object> deserialize,
            Func<object, bool> shouldSerialize = null)
        {
            var meta = dict[name];
            meta.serialize = serialize;
            meta.deserialize = deserialize;
            if (null != shouldSerialize)
                meta.shouldSerializeMethod = shouldSerialize;
        }

        public byte[] serialize(object val)
        {
            var props = list.Where(pm => pm.shouldSerialize(val)).ToArray();

            using (var stream = new MemoryStream())
            {
                using (var packer = Packer.Create(stream))
                {
                    serialize(val, packer);
                }
                return stream.ToArray();
            }
        }

        public void serialize(object val, Packer packer)
        {
            var props = list.Where(pm => pm.shouldSerialize(val)).ToArray();
            packer.PackMapHeader(props.Length);
            foreach (var meta in props)
            {
                packer.PackString(meta.name);
                meta.serialize(val, packer);
            }
        }

        public object deserialize(Unpacker unpacker)
        {
            var res = Activator.CreateInstance(type);

            long fieldCount = 0;
            unpacker.ReadMapLength(out fieldCount);
            for (var i = 0; i < fieldCount; i++)
            {
                string propName;
                unpacker.ReadString(out propName);
                PropertyMetadata meta;
                if (!dict.TryGetValue(propName, out meta))
                {
                    Logger.Warning("Deserializing {0}: unknown property {1}", type.FullName, propName);
                    MessagePackObject obj;
                    unpacker.ReadObject(out obj);
                    continue;
                }

                if (null == meta.deserialize)
                {
                    Logger.Error("Deserializing {0}: no deserializer for property {1}", type.FullName, propName);
                    MessagePackObject obj;
                    unpacker.ReadObject(out obj);
                    continue;
                }

                meta.deserialize(unpacker, res);
            }
            return res;
        }

        public object deserialize(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var unpacker = Unpacker.Create(stream))
            {
                return deserialize(unpacker);
            }
        }
    }
}