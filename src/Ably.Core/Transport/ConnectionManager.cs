﻿using System;
using System.Collections.Generic;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    public class ConnectionManager : IConnectionManager, ITransportListener, IConnectionContext
    {
        internal ConnectionManager()
        {
            this.sync = System.Threading.SynchronizationContext.Current;
            this.pendingMessages = new Queue<ProtocolMessage>();
        }

        internal ConnectionManager(ITransport transport, IAcknowledgementProcessor ackProcessor, States.Connection.ConnectionState initialState)
            : this()
        {
            this.transport = transport;
            this.transport.Listener = this;
            this.state = initialState;
            this.ackProcessor = ackProcessor;
            this.connection = new Connection(this);
        }

        public ConnectionManager(AblyRealtimeOptions options)
            : this()
        {
            this.options = options;
            this.state = new States.Connection.ConnectionInitializedState(this);
            this.ackProcessor = new AcknowledgementProcessor();
            this.connection = new Connection(this);
        }

        private ITransport transport;
        private System.Threading.SynchronizationContext sync;
        private Queue<ProtocolMessage> pendingMessages;
        private AblyRealtimeOptions options;
        private States.Connection.ConnectionState state;
        private IAcknowledgementProcessor ackProcessor;
        private DateTimeOffset? _firstConnectionAttempt;
        private int _connectionAttempts;
        private Connection connection;

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

        DateTimeOffset? IConnectionContext.FirstConnectionAttempt
        {
            get
            {
                return _firstConnectionAttempt;
            }
        }

        int IConnectionContext.ConnectionAttempts
        {
            get
            {
                return _connectionAttempts;
            }
        }

        public Connection Connection
        {
            get
            {
                return connection;
            }
        }

        public ConnectionState ConnectionState
        {
            get
            {
                return this.state.State;
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
            ackProcessor.SendMessage(message, callback);
            state.SendMessage(message);
        }

        public void Ping(Action<bool, ErrorInfo> callback)
        {
            ConnectionHeartbeatRequest.Execute(this, callback);
        }

        private TransportParams CreateTransportParameters(bool useFallbackHost)
        {
            return CreateTransportParameters(this.options, this.connection, useFallbackHost);
        }

        internal static TransportParams CreateTransportParameters(AblyRealtimeOptions options, Connection connection, bool useFallbackHost)
        {
            TransportParams transportParams = new TransportParams(options);
            transportParams.Host = GetHost(options, useFallbackHost);
            transportParams.Port = options.Tls ? Defaults.TlsPort : Transport.Defaults.Port;
            transportParams.FallbackHosts = Defaults.FallbackHosts;
            if (connection != null)
            {
                transportParams.ConnectionKey = connection.Key;
                transportParams.ConnectionSerial = connection.Serial.ToString();
            }
            return transportParams;
        }

        private static string GetHost(AblyRealtimeOptions options, bool useFallbackHost)
        {
            string defaultHost = Defaults.RealtimeHost;
            if (useFallbackHost)
            {
                Random r = new Random();
                defaultHost = Defaults.FallbackHosts[r.Next(0, 1000) % Defaults.FallbackHosts.Length];
            }
            string host = !string.IsNullOrEmpty(options.Host) ? options.Host : defaultHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        internal virtual ITransport CreateTransport(TransportParams transportParams)
        {
            return Defaults.TransportFactories["web_socket"].CreateTransport(transportParams);
        }

        //
        // Transport communication
        //
        void ITransportListener.OnTransportConnected()
        {
            if (this.sync != null )
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportConnected()), null);
                return;
            }
            this.OnTransportConnected();
        }

        void ITransportListener.OnTransportDisconnected()
        {
            if (this.sync != null )
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportDisconnected()), null);
                return;
            }
            this.OnTransportDisconnected();
        }

        void ITransportListener.OnTransportError(Exception e)
        {
            if (this.sync != null )
            {
                this.sync.Post(new System.Threading.SendOrPostCallback(o => this.OnTransportError((TransportState)o, e)), transport.State);
                return;
            }
            this.OnTransportError(transport.State, e);
        }

        void ITransportListener.OnTransportMessageReceived(ProtocolMessage message)
        {
            if (this.sync != null )
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
            Logger.Debug("ConnectionManager: Message Received {0}", message);

            bool handled = this.state.OnMessageReceived(message);
            handled |= this.ackProcessor.OnMessageReceived(message);
            handled |= ConnectionHeartbeatRequest.CanHandleMessage(message);

            if (message.connectionSerial != null)
            {
                this.connection.Serial = message.connectionSerial.Value;
            }

            if (this.MessageReceived != null)
            {
                this.MessageReceived(message);
            }
        }

        void IConnectionContext.SetState(States.Connection.ConnectionState newState)
        {
            this.state = newState;
            this.state.OnAttachedToContext();

            this.ackProcessor.OnStateChanged(newState);

            this.connection.OnStateChanged(newState.State, newState.Error, newState.RetryIn ?? -1);
        }

        void IConnectionContext.CreateTransport(bool useFallbackHost)
        {
            if (this.transport != null)
                (this as IConnectionContext).DestroyTransport();

            TransportParams transportParams = CreateTransportParameters(useFallbackHost);
            this.transport = CreateTransport(transportParams);
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

        void IConnectionContext.AttemptConnection()
        {
            if (_firstConnectionAttempt == null)
            {
                _firstConnectionAttempt = DateTimeOffset.Now;
            }
            _connectionAttempts++;
        }

        void IConnectionContext.ResetConnectionAttempts()
        {
            _firstConnectionAttempt = null;
            _connectionAttempts = 0;
        }
    }
}
