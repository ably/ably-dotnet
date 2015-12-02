using System.Security.Cryptography;
using Ably.Encryption;

namespace Ably
{
    public class CipherParams
    {
        public string Algorithm { get; private set; }
        public byte[] Key { get; private set; }
        public byte[] Iv { get; private set; }
        public int KeyLength { get; private set; }

        public string CipherType
        {
            get { return string.Format("{0}-{1}", Algorithm, KeyLength); }
        }

        public CipherParams(byte[] key) : this(Crypto.DefaultAlgorithm, key)
        {

        }

        public CipherParams(string algorithm, byte[] key, int? keyLength = null, byte[] iv = null)
        {
            Algorithm = algorithm.IsEmpty() ? Crypto.DefaultAlgorithm : algorithm;
            Key = key;
            KeyLength = keyLength ?? Crypto.DefaultKeylength;
            Iv = iv;
        }

    }
}