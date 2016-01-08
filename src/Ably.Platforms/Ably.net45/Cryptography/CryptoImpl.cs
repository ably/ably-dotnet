using Ably.Platform;
using System;
using System.Security.Cryptography;
using System.Text;
using Ably.Rest;

namespace Ably.Cryptography
{
    internal class CryptoImpl : ICrypto
    {
        string ICrypto.ComputeHMacSha256( string text, string key )
        {
            byte[] bytes = text.GetBytes();
            byte[] keyBytes = key.GetBytes();
            using( var hmac = new HMACSHA256( keyBytes ) )
            {
                hmac.ComputeHash( bytes );
                return Convert.ToBase64String( hmac.Hash );
            }
        }

        IChannelCipher ICrypto.GetCipher( ChannelOptions opts )
        {
            throw new NotImplementedException();
        }

        CipherParams ICrypto.GetDefaultParams()
        {
            throw new NotImplementedException();
        }
    }
}