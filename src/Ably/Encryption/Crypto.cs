using System;
using System.Net;
using System.Security.Cryptography;
using Ably.Rest;

namespace Ably.Encryption
{
    internal class Crypto
    {
        public const String DefaultAlgorithm = "AES";
        public const int DefaultKeylength = 128; // bits
        public const int DefaultBlocklength = 16; // bytes
#if !SILVERLIGHT
        public const CipherMode DefaultMode = CipherMode.CBC;
#endif

        public static CipherParams GetDefaultParams()
        {
#if SILVERLIGHT
            using (var aes = new AesManaged())
#else
            using (var aes = new AesCryptoServiceProvider())
#endif
            {
                aes.KeySize = DefaultKeylength;
#if !SILVERLIGHT
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
#endif
                aes.BlockSize = DefaultBlocklength * 8;
                aes.GenerateKey();
                return new CipherParams(aes.Key);
            }
        }

        public static CipherParams GetDefaultParams(byte[] key)
        {
            return new CipherParams(key);
        }

        public static IChannelCipher GetCipher(ChannelOptions opts)
        {
            CipherParams @params = opts.CipherParams ?? GetDefaultParams();

            if (string.Equals(@params.Algorithm, DefaultAlgorithm, StringComparison.CurrentCultureIgnoreCase))
                return new AesCipher(@params);

            throw new AblyException("Currently only the AES encryption algorith is supported", 50000, HttpStatusCode.InternalServerError);
        }
    }
}