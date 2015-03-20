using Ably.Types;
using System;

namespace Ably.Transport
{
    public interface ITransport
    {
        string Host { get; }

        ITransportListener Listener { get; set; }

        void Connect();

        void Close(bool sendDisconnect);

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
        void OnTransportError();
        void OnTransportMessageReceived(ProtocolMessage message);
    }
}
