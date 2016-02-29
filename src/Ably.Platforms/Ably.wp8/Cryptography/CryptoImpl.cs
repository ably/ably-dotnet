using Ably;
using Ably.Encryption;
using Ably.Platform;
using System;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography.Core;

namespace AblyPlatform.Cryptography
{
    internal class CryptoImpl : ICrypto
    {
        string ICrypto.ComputeHMacSha256( string text, string strKey )
        {
            byte[] bytes = text.GetBytes();
            byte[] keyBytes = strKey.GetBytes();

            // TODO: verify it produces same results as that different API in .NET 4
            MacAlgorithmProvider prov = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            CryptographicHash hash = prov.CreateHash(keyBytes.AsBuffer());
            hash.Append( bytes.AsBuffer() );
            byte[] hashBytes = hash.GetValueAndReset().ToArray();
            return Convert.ToBase64String( hashBytes );
        }

        IChannelCipher ICrypto.GetCipher( CipherParams p )
        {
            if( string.Equals( p.Algorithm, Crypto.DefaultAlgorithm, StringComparison.CurrentCultureIgnoreCase ) )
                return new AesCipher( p );

            throw new AblyException( "Currently only the AES encryption algorithm is supported", 50000, HttpStatusCode.InternalServerError );
        }

        CipherParams ICrypto.GetDefaultParams()
        {
            /* using( var aes = new AesCryptoServiceProvider() )
            {
                aes.KeySize = Crypto.DefaultKeylength;
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.BlockSize = Crypto.DefaultBlocklength * 8;
                aes.GenerateKey();
                return new CipherParams( aes.Key );
            } */
            throw new NotImplementedException();
        }
    }
}