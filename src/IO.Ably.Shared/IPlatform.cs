using IO.Ably.Transport;

namespace IO.Ably
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.DocumentationRules",
        "SA1600:Elements should be documented",
        Justification = "Internal interface")]
    internal interface IPlatform
    {
        string PlatformId { get; }

        ITransportFactory TransportFactory { get; }

        void RegisterOsNetworkStateChanged();
    }
}
