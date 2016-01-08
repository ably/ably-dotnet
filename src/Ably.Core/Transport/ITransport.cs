using Ably.Types;
using System;

namespace Ably.Transport
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

        void Abort(string reason);

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
