using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Transport
{
    public class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        internal ConnectionManager()
        {
            this.sync = System.Threading.SynchronizationContext.Current;
            this.pendingMessages = new Queue<ProtocolMessage>();
            this.ackQueue = new Dictionary<long, Action<bool, ErrorInfo>>();
            this.state = new States.Connection.ConnectionInitializedState(this);
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

        private ITransport transport;
        private System.Threading.SynchronizationContext sync;
        private ILogger Logger = Config.AblyLogger;
        private Queue<ProtocolMessage> pendingMessages;
        private long msgSerial;
        private Dictionary<long, Action<bool, ErrorInfo>> ackQueue;
        private AblyRealtimeOptions options;
        private States.Connection.ConnectionState state;

        public event StateChangedDelegate StateChanged;

        public event MessageReceivedDelegate MessageReceived;

        // TODO: Find out why is this?
        public bool IsActive
        {
            get { return false; }
        }

        States.Connection.ConnectionState IConnectionContext.State
        {
            get { return state; }
        }

        ITransport IConnectionContext.Transport
        {
            get
            {
                return this.transport;
            }
        }

        Queue<ProtocolMessage> IConnectionContext.QueuedMessages
        {
            get
            {
                return this.pendingMessages;
            }
        }

        public void Connect()
        {
            this.state.Connect();
        }

        public void Close()
        {
            this.state.Close();
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            message.MsgSerial = this.msgSerial++;

            if (callback != null)
            {
                this.ackQueue.Add(message.MsgSerial, callback);
            }
            state.SendMessage(message);
        }

        internal static TransportParams CreateTransportParameters(AblyRealtimeOptions options)
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
        // Transport communication
        //
        void ITransportListener.OnTransportConnected()
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportConnected()), null);
                return;
            }
            this.OnTransportConnected();
        }

        void ITransportListener.OnTransportDisconnected()
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportDisconnected()), null);
                return;
            }
            this.OnTransportDisconnected();
        }

        void ITransportListener.OnTransportError(Exception e)
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportError((TransportState)o, e)), transport.State);
                return;
            }
            this.OnTransportError(transport.State, e);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (this.sync != null && this.sync.IsWaitNotificationRequired())
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportMessageReceived(message)), null);
                return;
            }
            this.OnTransportMessageReceived(message);
        }

        private void OnTransportConnected()
        {
            this.state.OnTransportStateChanged(new States.Connection.ConnectionState.TransportStateInfo(TransportState.Connected));
        }

        private void OnTransportDisconnected()
        {
            this.state.OnTransportStateChanged(new States.Connection.ConnectionState.TransportStateInfo(TransportState.Closed));
        }

        private void OnTransportError(TransportState state, Exception e)
        {
            this.state.OnTransportStateChanged(new States.Connection.ConnectionState.TransportStateInfo(state, e));
        }

        private void OnTransportMessageReceived(ProtocolMessage message)
        {
            // If the state didn't handle the message, handle it here
            if (!this.state.OnMessageReceived(message))
            {
                ProcessProtocolMessage(message);
            }
        }

        void IConnectionContext.SetState(States.Connection.ConnectionState newState)
        {
            this.state = newState;
            this.state.OnAttachedToContext();

            if (this.StateChanged != null)
            {
                this.StateChanged(newState.State, newState.ConnectionInfo, newState.Error);
            }
        }

        void IConnectionContext.CreateTransport()
        {
            if (this.transport != null)
                (this as IConnectionContext).DestroyTransport();

            TransportParams transportParams = CreateTransportParameters(options);
            this.transport = Defaults.TransportFactories["web_socket"].CreateTransport(transportParams);
            this.transport.Listener = this;
        }

        void IConnectionContext.DestroyTransport()
        {
            if (this.transport == null)
                return;

            this.transport.Close();
            this.transport.Listener = null;
            this.transport = null;
        }

        private void ProcessProtocolMessage(ProtocolMessage message)
        {
            this.Logger.Verbose("ConnectionManager: Message Received {0}", message);

            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Heartbeat:
                    this.OnMessage_Heartbeat(message);
                    break;
                case ProtocolMessage.MessageAction.Ack:
                    this.HandleMessageAcknowledgement(message);
                    break;
                case ProtocolMessage.MessageAction.Nack:
                    this.HandleMessageAcknowledgement(message);
                    break;
                default:
                    if (this.MessageReceived != null)
                    {
                        this.MessageReceived(message);
                    }
                    break;
            }
        }

        private void OnMessage_Heartbeat(ProtocolMessage message)
        {
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
    }
}
