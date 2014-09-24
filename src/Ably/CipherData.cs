namespace Ably
{
    internal sealed class CipherData : TypedBuffer
    {
        public CipherData(byte[] cipherText, Ably.Protocol.TType type)
        {
            Buffer = cipherText;
            Type = type;
        }

        public CipherData(byte[] cipherText, int type)
            : this(cipherText, (Protocol.TType)type)
        {
        }
    }
}