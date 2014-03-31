namespace Ably
{
    public class CipherParams
    {
        public string Algorithm { get; private set; }
        public byte[] Key { get; private set; }

        public CipherParams(byte[] key)
        {
            Key = key;
        }

        public CipherParams(string algorithm, byte[] key)
        {
            Algorithm = algorithm.IsEmpty() ? Crypto.DefaultAlgorithm : algorithm;
            Key = key;
        }

    }
}