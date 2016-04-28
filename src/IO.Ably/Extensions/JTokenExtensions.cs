using System;
using Newtonsoft.Json.Linq;

namespace IO.Ably
{
    internal static class JTokenExtensions
    {
        internal static T OptValue<T>(this JToken token, string name)
        {
            var value = token[name];
            return value == null ? default(T) : value.ToObject<T>();
        }
    }
}