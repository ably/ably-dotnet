using System;
using System.Reflection;
using MsgPack;
using Newtonsoft.Json;

namespace IO.Ably.Types.MsgPack
{
    internal class PropertyMetadata
    {
        private readonly bool canRead;
        private readonly bool canWrite;
        public readonly string name;
        private readonly PropertyInfo pi;

        public Action<Unpacker, object> deserialize;
        public Action<object, Packer> serialize;
        public Func<object, bool> shouldSerializeMethod;

        public PropertyMetadata(PropertyInfo pi, string serializeName = null)
        {
            this.pi = pi;
            var propName = pi.Name;
            name = propName;

            var getter = pi.GetMethod;
            canRead = (null != getter) && getter.IsPublic;

            var setter = pi.SetMethod;
            canWrite = (null != setter) && setter.IsPublic;

            // Find prop name
            var jp = this.pi.GetCustomAttribute<JsonPropertyAttribute>();
            if (null != jp && jp.PropertyName.IsNotEmpty())
                name = jp.PropertyName;

            if (null != serializeName)
                name = serializeName;

            // Look for ShouldSerializeXxx method
            var ssm = "ShouldSerialize" + propName;
            var mi = this.pi.DeclaringType.GetRuntimeMethod(ssm, new Type[0]);
            if (null != mi)
                shouldSerializeMethod = obj => (bool) mi.Invoke(obj, null);

            // Fill those delegates
            if (pi.PropertyType == typeof (int) || pi.PropertyType.IsEnum)
            {
                deserialize = (unp, obj) =>
                {
                    int i;
                    unp.ReadInt32(out i);
                    pi.SetValue(obj, i);
                };
                serialize = (obj, p) => p.Pack((int) pi.GetValue(obj));
                return;
            }
            if (pi.PropertyType == typeof (long))
            {
                deserialize = (unp, obj) =>
                {
                    long i;
                    unp.ReadInt64(out i);
                    pi.SetValue(obj, i);
                };
                serialize = (obj, p) => p.Pack((long) pi.GetValue(obj));
                return;
            }
            if (pi.PropertyType == typeof (long?))
            {
                deserialize = (unp, obj) =>
                {
                    long? i;
                    unp.ReadNullableInt64(out i);
                    pi.SetValue(obj, i);
                };
                serialize = (obj, p) => p.Pack((long?) pi.GetValue(obj));
                return;
            }
            if (pi.PropertyType == typeof (string))
            {
                deserialize = (unp, obj) =>
                {
                    string s;
                    unp.ReadString(out s);
                    pi.SetValue(obj, s);
                };
                serialize = (obj, p) => p.Pack((string) pi.GetValue(obj));
            }
        }

        public bool shouldSerialize(object obj)
        {
            if (!canRead)
                return false;
            if (null == serialize)
                return false;

            if (null != shouldSerializeMethod)
                if (!shouldSerializeMethod(obj))
                    return false;

            var val = pi.GetValue(obj);
            if (null == val)
                return false;

            if (val is string && (string) val == "")
                return false;

            return true;
        }
    }
}