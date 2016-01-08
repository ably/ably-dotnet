using Ably.Rest;

namespace Ably.Platform
{
    public interface ICrypto
    {
        CipherParams GetDefaultParams();

        IChannelCipher GetCipher( ChannelOptions opts );

        string ComputeHMacSha256( string text, string key );
    }
}