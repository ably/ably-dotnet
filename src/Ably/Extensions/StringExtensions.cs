using System;
using System.Collections.Generic;
using System.Linq;
#if SILVERLIGHT
using SCS = Ably.Utils;
#else
using SCS = System.Collections.Specialized;
#endif

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

        public static SCS.NameValueCollection ParseQueryString(this string text)
        {
            if(text.IsEmpty())
                return new SCS.NameValueCollection();

            var queryParameters = new SCS.NameValueCollection();
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

        public static string JoinStrings(this IEnumerable<string> input, string delimiter = ", ")
        {
            return string.Join(delimiter, input.Where(x => x.IsNotEmpty()));
        }

        public static string Join<T>(this IEnumerable<T> listOfTs, Func<T, string> selector, string delimiter = ",") where T : class
        {
            if (listOfTs != null)
                return String.Join(delimiter, listOfTs.Select(selector));

            return string.Empty;
        }
    }
}
