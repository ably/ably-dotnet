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

        public ConnectionManager(AblyOptions options)
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
            this.transport.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Heartbeat));
        }

        public void Send(ProtocolMessage message)
        {
            this.transport.Send(message);
        }

        private static TransportParams CreateTransportParameters(AblyOptions options)
        {
            TransportParams transportParams = new TransportParams(options);
            transportParams.Host = Defaults.RealtimeHost;
            transportParams.Port = options.Tls ? Defaults.TlsPort : Transport.Defaults.Port;
            transportParams.FallbackHosts = Defaults.FallbackHosts;
            return transportParams;
        }

        //
        // ConnectionManager communication
        //
        private void OnStateChanged(ConnectionState state, ConnectionInfo info = null)
        {
            if (this.StateChanged != null)
            {
                this.StateChanged(state, info);
            }
        }

        private void OnMessageReceived(ProtocolMessage message)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(message);
            }
        }

        //
        // Transport communication
        //
        void ITransportListener.OnTransportConnected()
        {
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
            this.sync.Send(new System.Threading.SendOrPostCallback(o => this.ProcessProtocolMessage(message)), null);
        }

        private void ProcessProtocolMessage(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Heartbeat:
                    this.OnMessage_Heartbeat(message);
                    break;
                case ProtocolMessage.MessageAction.Error:
                    this.OnMessage_Error(message);
                    break;
                case ProtocolMessage.MessageAction.Connected:
                    this.OnMessage_Connected(message);
                    break;
                case ProtocolMessage.MessageAction.Disconnect:
                    this.OnMessage_Disconnected(message);
                    break;
                case ProtocolMessage.MessageAction.Closed:
                    this.OnMessage_Closed(message);
                    break;
                case ProtocolMessage.MessageAction.Ack:
                    this.OnMessage_Ack(message);
                    break;
                case ProtocolMessage.MessageAction.Nack:
                    this.OnMessage_Nack(message);
                    break;
                default:
                    this.OnMessageReceived(message);
                    break;
            }
        }

        private void OnMessage_Heartbeat(ProtocolMessage message)
        {
        }

        private void OnMessage_Error(ProtocolMessage message)
        {
            this.OnStateChanged(ConnectionState.Failed);
        }

        private void OnMessage_Connected(ProtocolMessage message)
        {
            ConnectionInfo info = new ConnectionInfo()
            {
                ConnectionId = message.ConnectionId,
                ConnectionKey = message.ConnectionKey,
                ConnectionSerial = message.ConnectionSerial
            };
            this.OnStateChanged(ConnectionState.Connected, info);
        }

        private void OnMessage_Disconnected(ProtocolMessage message)
        {
            this.OnStateChanged(ConnectionState.Disconnected);
        }

        private void OnMessage_Closed(ProtocolMessage message)
        {
            if (message.Error != null)
            {
                this.OnStateChanged(ConnectionState.Failed);
            }
            else
            {
                this.OnStateChanged(ConnectionState.Closed);
            }
        }

        private void OnMessage_Ack(ProtocolMessage message)
        {
        }

        private void OnMessage_Nack(ProtocolMessage message)
        {
        }
    }
}
