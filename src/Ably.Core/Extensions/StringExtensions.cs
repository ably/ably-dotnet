using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace IO.Ably
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
            if (IsEmpty(input))
                return false;

            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }

        public static string JoinStrings(this IEnumerable<string> input, string delimiter = ", ")
        {
            return string.Join(delimiter, input.Where(x => IsNotEmpty(x)));
        }

        public static string Join<T>(this IEnumerable<T> listOfTs, Func<T, string> selector, string delimiter = ",") where T : class
        {
            if (listOfTs != null)
                return String.Join(delimiter, listOfTs.Select(selector));

            return string.Empty;
        }
    }
}
