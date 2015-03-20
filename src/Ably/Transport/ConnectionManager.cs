using Ably.Realtime;
using Ably.Types;
using System;

namespace Ably.Transport
{
    public class ConnectionManager : IConnectionManager, ITransportListener
    {
        internal ConnectionManager()
        {
            this.sync = System.Threading.SynchronizationContext.Current;
        }

        internal ConnectionManager(ITransport transport)
            : this()
        {
            this.transport = transport;
        }

        public ConnectionManager(Options options)
            : this()
        {
            TransportParams transportParams = CreateTransportParameters(options);
            this.transport = Defaults.TransportFactories["web_socket"].CreateTransport(transportParams);
            this.transport.Listener = this;
        }

        internal ITransport transport;
        private System.Threading.SynchronizationContext sync;

        public event StateChangedDelegate StateChanged;

        public event MessageReceivedDelegate MessageReceived;

        public bool IsActive
        {
            get { return false; }
        }

        public void Connect()
        {
            this.transport.Connect();
        }

        public void Close()
        {
            this.transport.Close(true);
        }

        public void Ping()
        {
            // TODO: Implement
        }

        public void Send(ProtocolMessage message)
        {
            // TODO: Implement
        }

        private static TransportParams CreateTransportParameters(Options options)
        {
            TransportParams transportParams = new TransportParams(options);
            transportParams.Host = Defaults.RealtimeHost;
            transportParams.Port = options.Tls ? Defaults.TlsPort : Transport.Defaults.Port;
            transportParams.FallbackHosts = Defaults.FallbackHosts;
            return transportParams;
        }

        void ITransportListener.OnTransportConnected()
        {
            this.sync.Send(new System.Threading.SendOrPostCallback(o => this.OnStateChanged(ConnectionState.Connected)), null);
        }

        void ITransportListener.OnTransportDisconnected()
        {
            this.sync.Send(new System.Threading.SendOrPostCallback(o => this.OnStateChanged(ConnectionState.Disconnected)), null);
        }

        void ITransportListener.OnTransportError()
        {
            this.sync.Send(new System.Threading.SendOrPostCallback(o => this.OnStateChanged(ConnectionState.Failed)), null);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            this.sync.Send(new System.Threading.SendOrPostCallback(o => this.OnMessageReceived()), null);
        }

        private void OnStateChanged(ConnectionState state)
        {
            if (this.StateChanged != null)
            {
                this.StateChanged(state);
            }
        }

        private void OnMessageReceived()
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived();
            }
        }
    }
}
