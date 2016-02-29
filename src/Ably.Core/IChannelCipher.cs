namespace Ably
{
    public interface IChannelCipher
    {
        string Algorithm { get; }
        byte[] Encrypt(byte[] input);
        byte[] Decrypt(byte[] input);
    }
}