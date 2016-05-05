using System;
using System.Threading.Tasks;
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
        string Host { get; }

        TransportState State { get; }

        ITransportListener Listener { get; set; }

        void Connect();

        void Close();

        void Send(ProtocolMessage message);
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
        void OnTransportMessageReceived(ProtocolMessage message);
    }
}
