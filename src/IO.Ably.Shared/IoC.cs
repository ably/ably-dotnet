namespace IO.Ably
{
    /// <summary>This class initializes Platform.</summary>
    internal class IoC
    {
        public static readonly Platform Platform = new Platform();

        public static Agent.PlatformRuntime PlatformId => Platform?.PlatformId ?? Agent.PlatformRuntime.Other;

        public static void RegisterOsNetworkStateChanged(ILogger logger) => Platform.RegisterOsNetworkStateChanged(logger);
    }
}
