using System;
using System.Collections.Generic;
using System.Linq;

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

        internal static bool IsNotEmpty(object nonce)
        {
            throw new NotImplementedException();
        }

        public static string JoinStrings(this IEnumerable<string> input, string delimiter = ", ")
        {
            if (input == null) return "";

            return string.Join(delimiter, input.Where(IsNotEmpty));
        }

        public static string Join<T>(this IEnumerable<T> listOfTs, Func<T, string> selector, string delimiter = ",") where T : class
        {
            if (listOfTs != null)
                return String.Join(delimiter, listOfTs.Select(selector));

            return string.Empty;
        }

        public static bool EqualsTo(this string input, string other, bool caseSensitive = false)
        {
            return string.Equals(input, other,
                caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
