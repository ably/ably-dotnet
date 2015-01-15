namespace Ably
{
    internal sealed class CipherData : TypedBuffer
    {
        public CipherData(byte[] cipherText)
        {
            Buffer = cipherText;
        }

        public CipherData(byte[] cipherText, int type)
            : this(cipherText)
        {
        }
    }
}