using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace IO.Ably
{
    public static class DictionaryExtensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return default(TValue);
        }
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return defaultValue;
        }

        public static string ToQueryString(this Dictionary<string,string> @params)
        {
            if (@params == null || @params.Count == 0)
            {
                return "";
            }

            return string.Join("&", @params.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));
        }

        public static Dictionary<string, string> Merge(this Dictionary<string, string> first, Dictionary<string, string> second)
        {
            if (second == null)
            {
                return first;
            }

            if (first == null)
            {
                first = new Dictionary<string,string>();
            }

            var result = first.ToDictionary(x => x.Key, x => x.Value);
            foreach(var item in second)
            {
                if (result.Keys.Any(x => string.Equals(x, item.Key, StringComparison.OrdinalIgnoreCase)) ==
                    false)
                {
                    result.Add(item.Key, item.Value);
                }
            }

            return result;
        }
    }
}
