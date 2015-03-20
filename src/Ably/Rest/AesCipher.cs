using System;
using System.Linq;
using System.Security.Cryptography;

namespace Ably
{
    public class AesCipher : IChannelCipher
    {
        private readonly CipherParams _params;

        public AesCipher(CipherParams @params)
        {
            _params = @params;
        }

        private static byte[] Encrypt(byte[] input, byte[] key, int keySize)
        {
            ICryptoTransform crypto;
            using (var aesEncryption = new RijndaelManaged())
            {
                aesEncryption.GenerateIV();
                aesEncryption.KeySize = keySize;
                aesEncryption.BlockSize = 128;
                aesEncryption.Mode = CipherMode.CBC;
                aesEncryption.Padding = PaddingMode.PKCS7;
                aesEncryption.IV = aesEncryption.IV;
                aesEncryption.Key = key;
                crypto = aesEncryption.CreateEncryptor();

                // The result of the encryption and decryption            
                byte[] cipherText = crypto.TransformFinalBlock(input, 0, input.Length);
                var result = new byte[cipherText.Length + aesEncryption.IV.Length];
                Buffer.BlockCopy(aesEncryption.IV, 0, result, 0, aesEncryption.IV.Length);
                Buffer.BlockCopy(cipherText, 0, result, aesEncryption.IV.Length, cipherText.Length);
                return result;

            }
        }

        private static byte[] Decrypt(byte[] input, byte[] key, int keySize)
        {
            //byte[] encryptedBytes = Convert.FromBase64CharArray(encryptedText.ToCharArray(), 0, encryptedText.Length);
            byte[] iv = input.Take(keySize / 8).ToArray();
            using (var aesEncryption = new RijndaelManaged())
            {
                aesEncryption.KeySize = keySize;
                aesEncryption.BlockSize = 128;
                aesEncryption.Mode = CipherMode.CBC;
                aesEncryption.Padding = PaddingMode.PKCS7;
                aesEncryption.IV = iv;
                aesEncryption.Key = key;
                ICryptoTransform decrypt = aesEncryption.CreateDecryptor();
                return decrypt.TransformFinalBlock(input.Skip(keySize / 8).ToArray(), 0, input.Length - keySize / 8);
            }
        }

        public byte[] Encrypt(byte[] input)
        {
            try
            {
                return Encrypt(input, _params.Key, Crypto.DefaultKeylength);
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
                return Decrypt(input, _params.Key, Crypto.DefaultKeylength);
            }
            catch (Exception ex)
            {
                throw new AblyException(ex);
            }
        }
    }
}