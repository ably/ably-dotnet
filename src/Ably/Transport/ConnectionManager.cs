using Ably.Realtime;
using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Transport
{
    public class ConnectionManager : IConnectionManager, ITransportListener
    {
        internal ConnectionManager()
        {
            this.sync = System.Threading.SynchronizationContext.Current;
            this.pendingMessages = new Queue<ProtocolMessage>();
        }

        internal ConnectionManager(ITransport transport)
            : this()
        {
            this.transport = transport;
            this.transport.Listener = this;
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
        private ILogger Logger = Config.AblyLogger;
        private Queue<ProtocolMessage> pendingMessages;

        public event StateChangedDelegate StateChanged;

        public event MessageReceivedDelegate MessageReceived;

        public bool IsActive
        {
            get { return false; }
        }

        public void Connect()
        {
            if (this.transport.State == TransportState.Initialized || this.transport.State == TransportState.Closed)
            {
                this.transport.Connect();
            }
        }

        public void Close()
        {
            this.transport.Close(this.transport.State == TransportState.Connected);
        }

        public void Send(ProtocolMessage message, Action<ErrorInfo> callback)
        {
            // TODO: Implement callback
            if (this.transport.State == TransportState.Connected)
            {
                this.SendDirect(message);
            }
            else
            {
                this.pendingMessages.Enqueue(message);
            }
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
        private void SetState(object state)
        {
            this.SetState((ConnectionState)state);
        }

        private void SetState(ConnectionState state, ConnectionInfo info = null, ErrorInfo error = null)
        {
            this.Logger.Info("ConnectionManager: StateChanged: {0}", state);
            if (this.StateChanged != null)
            {
                this.StateChanged(state, info, error);
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
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Send(new System.Threading.SendOrPostCallback(o => this.OnTransportConnected()), null);
            }
            else
            {
                this.OnTransportConnected();
            }
        }

        void ITransportListener.OnTransportDisconnected()
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Send(new System.Threading.SendOrPostCallback(this.SetState), ConnectionState.Disconnected);
            }
            else
            {
                this.SetState(ConnectionState.Disconnected);
            }
        }

        void ITransportListener.OnTransportError()
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Send(new System.Threading.SendOrPostCallback(this.SetState), ConnectionState.Failed);
            }
            else
            {
                this.SetState(ConnectionState.Failed);
            }
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Send(new System.Threading.SendOrPostCallback(this.ProcessProtocolMessage), message);
            }
            else
            {
                this.ProcessProtocolMessage(message);
            }
        }

        private void OnTransportConnected()
        {
            this.Logger.Info("ConnectionManager: Transport Connected");
            foreach (ProtocolMessage message in this.pendingMessages)
            {
                this.SendDirect(message);
            }
            this.pendingMessages.Clear();
        }

        private void ProcessProtocolMessage(object message)
        {
            this.ProcessProtocolMessage(message as ProtocolMessage);
        }

        private void ProcessProtocolMessage(ProtocolMessage message)
        {
            this.Logger.Verbose("ConnectionManager: Message Received {0}", message);

            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Heartbeat:
                    this.OnMessage_Heartbeat(message);
                    break;
                case ProtocolMessage.MessageAction.Error:
                    AblyException transportException = new AblyException(message.Error);
                    if (message.Error == null)
                    {
                        this.Logger.Error("OnTransportMessageReceived(): ERROR message received", transportException);
                    }
                    if (!string.IsNullOrEmpty(message.Channel))
                    {
                        OnMessageReceived(message);
                    }
                    else
                    {
                        OnMessage_Error(message);
                    }
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
            this.SetState(ConnectionState.Failed, error: message.Error);
        }

        private void OnMessage_Connected(ProtocolMessage message)
        {
            ConnectionInfo info = new ConnectionInfo(message.ConnectionId, message.ConnectionSerial, message.ConnectionKey);
            this.SetState(ConnectionState.Connected, info: info, error: message.Error);
        }

        private void OnMessage_Disconnected(ProtocolMessage message)
        {
            this.SetState(ConnectionState.Disconnected, error: message.Error);
        }

        private void OnMessage_Closed(ProtocolMessage message)
        {
            if (message.Error != null)
            {
                this.SetState(ConnectionState.Failed, error: message.Error);
            }
            else
            {
                this.SetState(ConnectionState.Closed);
            }
        }

        private void OnMessage_Ack(ProtocolMessage message)
        {
        }

        private void OnMessage_Nack(ProtocolMessage message)
        {
        }

        private void SendDirect(ProtocolMessage message)
        {
            this.Logger.Info("ConnectionManager: Sending Message: {0}", message);
            this.transport.Send(message);
        }
    }
}
