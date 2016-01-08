using Ably.Encryption;
using Ably.Platform;
using System;
using System.Net;
using System.Security.Cryptography;
using Ably;

namespace AblyPlatform.Cryptography
{
    internal class CryptoImpl : ICrypto
    {
        public IChannelCipher GetCipher( CipherParams p )
        {
            throw new NotImplementedException();
        }

        public CipherParams GetDefaultParams()
        {
            throw new NotImplementedException();
        }

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

        IChannelCipher ICrypto.GetCipher( CipherParams p )
        {
            if( string.Equals( p.Algorithm, Crypto.DefaultAlgorithm, StringComparison.CurrentCultureIgnoreCase ) )
                return new AesCipher( p );

            throw new AblyException( "Currently only the AES encryption algorithm is supported", 50000, HttpStatusCode.InternalServerError );
        }

        CipherParams ICrypto.GetDefaultParams()
        {
            using( var aes = new AesCryptoServiceProvider() )
            {
                aes.KeySize = Crypto.DefaultKeylength;
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.BlockSize = Crypto.DefaultBlocklength * 8;
                aes.GenerateKey();
                return new CipherParams( aes.Key );
            }
        }
    }
}