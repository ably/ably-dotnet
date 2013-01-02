using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ably
{
    public static class StringUtils
    {

        /// <summary>
        /// Computes a HMAC SHA256 Hash and return the result as base64 encoded string
        /// </summary>
        /// <param name="text">string for which the hash is computed</param>
        /// <returns>Base64 encoded string of computed HMAC SHA256 hash</returns>
        public static string ComputeHMacSha256(this string text, string key)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hmac.Hash);
            }
        }
    }
}
