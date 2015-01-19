using System;
using System.Linq;
using System.Security.Cryptography;

namespace Ably
{
    internal static class DateService
    {
        internal static Func<DateTimeOffset> Now = () => DateTimeOffset.UtcNow;

        internal static long NowInUnixMilliseconds 
        {
            get { return Now().ToUnixTimeInMilliseconds(); }
        }

        internal static long NowInUnixSecond
        {
            get { return Now().ToUnixTime(); }
        }
    }

    public class AesCipher : IChannelCipher
    {
        private readonly CipherParams _params;

        public AesCipher(CipherParams @params)
        {
            _params = @params;
        }

        private static byte[] Encrypt(byte[] input, byte[] key, int keySize, CipherMode mode)
        {
            using (var aesEncryption = new RijndaelManaged())
            {
                aesEncryption.GenerateIV();
                aesEncryption.KeySize = keySize;
                aesEncryption.BlockSize = Crypto.DefaultBlocklength * 8;
                aesEncryption.Mode = mode;
                aesEncryption.Padding = PaddingMode.PKCS7;
                aesEncryption.IV = aesEncryption.IV;
                aesEncryption.Key = key;
                ICryptoTransform crypto = aesEncryption.CreateEncryptor();

                // The result of the encryption and decryption
                byte[] cipherText = crypto.TransformFinalBlock(input, 0, input.Length);
                var result = new byte[cipherText.Length + aesEncryption.IV.Length];
                Buffer.BlockCopy(aesEncryption.IV, 0, result, 0, aesEncryption.IV.Length);
                Buffer.BlockCopy(cipherText, 0, result, aesEncryption.IV.Length, cipherText.Length);
                return result;
            }
        }

        private static byte[] Decrypt(byte[] input, byte[] key, int keySize, CipherMode mode)
        {
            byte[] iv = input.Take(Crypto.DefaultBlocklength).ToArray();
            using (var aesEncryption = new RijndaelManaged())
            {
                aesEncryption.KeySize = keySize;
                aesEncryption.BlockSize = Crypto.DefaultBlocklength * 8;
                aesEncryption.Mode = mode;
                aesEncryption.Padding = PaddingMode.PKCS7;
                aesEncryption.IV = iv;
                aesEncryption.Key = key;

                ICryptoTransform decrypt = aesEncryption.CreateDecryptor();
                var encryptedBuffer = input.Skip(Crypto.DefaultBlocklength).ToArray();
                return decrypt.TransformFinalBlock(encryptedBuffer, 0, encryptedBuffer.Length);
            }
        }

        public string Algorithm
        {
            get { return "AES"; }
        }

        public byte[] Encrypt(byte[] input)
        {
            try
            {
                return Encrypt(input, _params.Key, _params.KeyLength, _params.Mode);
            }
            catch (Exception ex)
            {
                throw new AblyException(ex);
            }
        }

        public byte[] Decrypt(byte[] input)
        {
            try
            {
                return Decrypt(input, _params.Key, _params.KeyLength, _params.Mode);
            }
            catch (Exception ex)
            {
                throw new AblyException(ex);
            }
        }
    }
}