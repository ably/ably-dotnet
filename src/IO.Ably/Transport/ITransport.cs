using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

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

    public interface ITransport
    {
        TransportState State { get; }

        ITransportListener Listener { get; set; }

        void Connect();

        void Close();

        void Send(RealtimeTransportData data);
    }

    public interface ITransportFactory
    {
        ITransport CreateTransport(TransportParams parameters);
    }

    public interface ITransportListener
    {
        void OnTransportConnected();
        void OnTransportDisconnected();
        void OnTransportError(Exception error);
        void OnTransportDataReceived(RealtimeTransportData data);
    }
}
