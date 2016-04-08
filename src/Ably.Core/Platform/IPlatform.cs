using IO.Ably.Transport;

namespace IO.Ably.Platform
{
    /// <summary>This interface abstract platform-specific parts of the functionality, hard to implement in this portable library.</summary>
    /// <remarks>This interface must be implemented by AblyPlatform.PlatformImpl type, from the correct platform-specific AblyPlatform.dll library that must be referenced from your app.</remarks>
    public interface IPlatform
    {
        string GetConnectionString();

        ICrypto GetCrypto();

        ITransportFactory GetWebSocketsFactory();
    }
}