using System;
using IO.Ably.Realtime;

namespace IO.Ably.Transport
{
    public enum TransportState
    {
        Initialized,
        Connecting,
        Connected,
        Closing,
        Closed,
    }

    public interface ITransport : IDisposable
    {
        TransportState State { get; }

        ITransportListener Listener { get; set; }

        void Connect();

        void Close(bool suppressClosedEvent = true);

        void Send(RealtimeTransportData data);
    }

    public interface ITransportFactory
    {
        ITransport CreateTransport(TransportParams parameters);
    }

    public interface ITransportListener
    {
        void OnTransportDataReceived(RealtimeTransportData data);
        void OnTransportEvent(TransportState state, Exception exception = null);
    }
}
