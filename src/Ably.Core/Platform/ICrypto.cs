using Ably.Rest;

namespace Ably.Platform
{
    public interface ICrypto
    {
        CipherParams GetDefaultParams();

        IChannelCipher GetCipher( CipherParams p );

        string ComputeHMacSha256( string text, string key );
    }
}