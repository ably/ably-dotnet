using System;
using System.Collections.Specialized;

namespace Ably
{
    internal static class StringExtensions
    {
        public static bool IsNotEmpty(this string text)
        {
            return string.IsNullOrEmpty(text) == false;
        }

        public static bool IsEmpty(this string text)
        {
            return string.IsNullOrEmpty(text);
        }

        public static bool IsJson(this string input)
        {
            if (input.IsEmpty())
                return false;

            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }

        public static NameValueCollection ParseQueryString(this string text)
        {
            if(text.IsEmpty())
                return new NameValueCollection();

            var queryParameters = new NameValueCollection();
            string[] querySegments = text.Split('&');
            foreach (string segment in querySegments)
            {
                string[] parts = segment.Split('=');
                if (parts.Length > 0)
                {
                    string key = parts[0].Trim(new char[] { '?', ' ' });
                    string val = parts[1].Trim();

                    queryParameters.Add(key, Uri.UnescapeDataString(val));
                }
            }
            return queryParameters;
        }
    }
}
