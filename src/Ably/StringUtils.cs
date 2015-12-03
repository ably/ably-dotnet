using System;
using System.Security.Cryptography;
using System.Text;

namespace Ably
{
    internal static class StringUtils
    {

        /// <summary>
        /// Computes a HMAC SHA256 Hash and return the result as base64 encoded string
        /// </summary>
        /// <param name="text">string for which the hash is computed</param>
        /// <returns>Base64 encoded string of computed HMAC SHA256 hash</returns>
        internal static string ComputeHMacSha256(this string text, string key)
        {
            byte[] bytes = text.GetBytes();
            byte[] keyBytes = key.GetBytes();
            using (var hmac = new HMACSHA256(keyBytes))
            {
                hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hmac.Hash);
            }
        }

        internal static byte[] GetBytes(this string text)
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
            if(base64String.IsEmpty())
                return new byte[0];
            return Convert.FromBase64String(base64String);
        }

        internal static string EncodeUriPart(this string text)
        {
            return Uri.EscapeDataString(text);
        }
    }
}
