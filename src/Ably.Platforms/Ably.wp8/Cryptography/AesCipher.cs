using Ably;
using System;

namespace AblyPlatform.Cryptography
{
    /// <summary>Cipher implementation using RinjaelManaged class under the hood.
    /// The Cipher params decide the Cipher mode and key
    /// The Iv vector is generated on each encryption request and added to the encrypted data stream.</summary>
    internal class AesCipher : IChannelCipher
    {
        private readonly CipherParams _params;

        /// <summary>Create a new instance of AesCipther.</summary>
        /// <param name="params">Cipher params used to configure the RinjaelManaged algorithm</param>
        public AesCipher( CipherParams @params )
        {
            _params = @params;
        }

        public string Algorithm
        {
            get { return "AES"; }
        }

        /// <summary>Encrypt a byte[] using the CipherParams provided in the constructor</summary>
        /// <param name="input">byte[] to be encrypted</param>
        /// <returns>Encrypted result</returns>
        public byte[] Encrypt( byte[] input )
        {
            throw new NotImplementedException();
        }

        /// <summary>Decrypt an encrypted byte[] using the CipherParams provided in the constructor</summary>
        /// <param name="input">encrypted byte[]</param>
        /// <returns>decrypted byte[]</returns>
        public byte[] Decrypt( byte[] input )
        {
            throw new NotImplementedException();
        }
    }
}