namespace Ably
{
    public interface IChannelCipher
    {
        byte[] Encrypt(byte[] input);
        byte[] Decrypt(byte[] input);
    }
}