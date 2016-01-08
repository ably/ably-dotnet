using Ably.Platform;

namespace Ably
{
    public class PlatformImpl : IPlatform
    {
        ICrypto IPlatform.crypto
        {
            get
            {
                return new Cryptography.CryptoImpl();
            }
        }
    }
}