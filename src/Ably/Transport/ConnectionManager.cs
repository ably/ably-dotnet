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
            this.ackQueue = new Dictionary<long, Action<bool, ErrorInfo>>();
        }

        internal ConnectionManager(ITransport transport)
            : this()
        {
            this.transport = transport;
            this.transport.Listener = this;
        }

        public ConnectionManager(AblyRealtimeOptions options)
            : this()
        {
            this.options = options;
        }

        internal ITransport transport;
        private System.Threading.SynchronizationContext sync;
        private ILogger Logger = Config.AblyLogger;
        private Queue<ProtocolMessage> pendingMessages;
        private ConnectionState connectionState;
        private long msgSerial;
        private Dictionary<long, Action<bool, ErrorInfo>> ackQueue;
        private AblyRealtimeOptions options;

        public event StateChangedDelegate StateChanged;

        public event MessageReceivedDelegate MessageReceived;

        public bool IsActive
        {
            get { return false; }
        }

        public void Connect()
        {
            if (this.transport == null)
            {
                TransportParams transportParams = CreateTransportParameters(options);
                this.transport = Defaults.TransportFactories["web_socket"].CreateTransport(transportParams);
                this.transport.Listener = this;
            }

            if (this.transport.State == TransportState.Initialized || this.transport.State == TransportState.Closed)
            {
                this.transport.Connect();
            }
        }

        public void Close()
        {
            this.transport.Close(this.transport.State == TransportState.Connected);
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            message.MsgSerial = this.msgSerial++;
            if (callback != null)
            {
                this.ackQueue.Add(message.MsgSerial, callback);
            }
            if (this.transport.State == TransportState.Connected)
            {
                this.SendDirect(message);
            }
            else
            {
                this.pendingMessages.Enqueue(message);
            }
        }

        private static TransportParams CreateTransportParameters(AblyRealtimeOptions options)
        {
            TransportParams transportParams = new TransportParams(options);
            transportParams.Host = GetHost(options);
            transportParams.Port = options.Tls ? Defaults.TlsPort : Transport.Defaults.Port;
            transportParams.FallbackHosts = Defaults.FallbackHosts;
            return transportParams;
        }

        private static string GetHost(AblyRealtimeOptions options)
        {
            string host = !string.IsNullOrEmpty(options.Host) ? options.Host : Defaults.RealtimeHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        //
        // ConnectionManager communication
        //
        private void SetState(object state)
        {
            object[] stateArgs = (object[])state;
            this.SetState((ConnectionState)stateArgs[0], (ConnectionInfo)stateArgs[1], (ErrorInfo)stateArgs[2]);
        }

        private void SetState(ConnectionState state, ConnectionInfo info = null, ErrorInfo error = null)
        {
            this.Logger.Info("ConnectionManager: StateChanged: {0}", state);
            this.connectionState = state;
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
                this.sync.Send(new System.Threading.SendOrPostCallback(o => this.ProcessTransportDisconnected()), null);
            }
            else
            {
                this.ProcessTransportDisconnected();
            }
        }

        void ITransportListener.OnTransportError(Exception e)
        {
            ErrorInfo error = new ErrorInfo(e.Message, 80000, System.Net.HttpStatusCode.ServiceUnavailable);

            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Send(new System.Threading.SendOrPostCallback(this.SetState), new object[] { ConnectionState.Failed, null, error });
            }
            else
            {
                this.SetState(ConnectionState.Failed, error: error);
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
                    this.HandleMessageAcknowledgement(message);
                    break;
                case ProtocolMessage.MessageAction.Nack:
                    this.HandleMessageAcknowledgement(message);
                    break;
                default:
                    this.OnMessageReceived(message);
                    break;
            }
        }

        private void ProcessTransportDisconnected()
        {
            if (this.connectionState == ConnectionState.Closed)
            {
                return;
            }

            this.SetState(ConnectionState.Disconnected);
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
            this.ResetMsgAcknowledgement();
            this.SetState(ConnectionState.Connected, info: info, error: message.Error);
        }

        private void OnMessage_Disconnected(ProtocolMessage message)
        {
            this.SetState(ConnectionState.Disconnected, error: message.Error);
        }

        private void OnMessage_Closed(ProtocolMessage message)
        {
            this.ResetMsgAcknowledgement();
            if (message.Error != null)
            {
                this.SetState(ConnectionState.Failed, error: message.Error);
            }
            else
            {
                this.SetState(ConnectionState.Closed);
            }
        }

        private void ResetMsgAcknowledgement()
        {
            this.msgSerial = 0;
            this.ackQueue.Clear();
        }

        private void HandleMessageAcknowledgement(ProtocolMessage message)
        {
            long startSerial = message.MsgSerial;
            long endSerial = message.MsgSerial + (message.Count - 1);
            ErrorInfo reason = new ErrorInfo("Unknown error", 50000, System.Net.HttpStatusCode.InternalServerError);
            for (long i = startSerial; i <= endSerial; i++)
            {
                Action<bool, ErrorInfo> callback;
                if (this.ackQueue.TryGetValue(i, out callback))
                {
                    if (message.Action == ProtocolMessage.MessageAction.Ack)
                    {
                        callback(true, null);
                    }
                    else
                    {
                        callback(false, message.Error ?? reason);
                    }
                    this.ackQueue.Remove(i);
                }
            }
        }

        private void SendDirect(ProtocolMessage message)
        {
            this.Logger.Info("ConnectionManager: Sending Message: {0}", message);
            this.transport.Send(message);
        }
    }
}
