using System;
using System.Text;

namespace IO.Ably
{
    public static class StringUtils
    {
        public static byte[] GetBytes(this string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        internal static string GetText(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        internal static string ToBase64(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        internal static string ToBase64(this string text)
        {
            if (text.IsEmpty())
                return string.Empty;
            return text.GetBytes().ToBase64();
        }

        internal static byte[] FromBase64(this string base64String)
        {
            if (base64String.IsEmpty())
                return new byte[0];
            return Convert.FromBase64String(base64String);
        }

        internal static string EncodeUriPart(this string text)
        {
            return Uri.EscapeDataString(text);
        }
    }
}