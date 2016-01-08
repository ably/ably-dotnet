using System;
using System.Security.Cryptography;
using System.Text;

namespace Ably
{
    static class Misc
    {
        /// <summary>
        /// Computes a HMAC SHA256 Hash and return the result as base64 encoded string
        /// </summary>
        /// <param name="text">string for which the hash is computed</param>
        /// <returns>Base64 encoded string of computed HMAC SHA256 hash</returns>
        internal static string ComputeHMacSha256( this string text, string key )
        {
            byte[] bytes = text.GetBytes();
            byte[] keyBytes = key.GetBytes();
            using( var hmac = new HMACSHA256( keyBytes ) )
            {
                hmac.ComputeHash( bytes );
                return Convert.ToBase64String( hmac.Hash );
            }
        }
    }
}