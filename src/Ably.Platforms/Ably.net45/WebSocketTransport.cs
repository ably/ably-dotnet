using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using WebSocket4Net;

namespace AblyPlatform
{
    internal class WebSocketTransport : ITransport
    {
        public class WebSocketTransportFactory : ITransportFactory
        {
            public ITransport CreateTransport( TransportParams parameters )
            {
                IMessageSerializer serializer = null;
                if( parameters.Options.UseBinaryProtocol )
                {
                    serializer = new MsgPackMessageSerializer();
                }
                else
                {
                    serializer = new JsonMessageSerializer();
                }
                WebSocketTransport socketTransport = new WebSocketTransport(serializer);
                socketTransport.Host = parameters.Host;
                socketTransport.channelBinaryMode = parameters.Options.UseBinaryProtocol;
                socketTransport.socket = CreateSocket( parameters );
                socketTransport.socket.Opened += socketTransport.socket_Opened;
                socketTransport.socket.Closed += socketTransport.socket_Closed;
                socketTransport.socket.Error += socketTransport.socket_Error;
                socketTransport.socket.MessageReceived += socketTransport.socket_MessageReceived;
                socketTransport.socket.DataReceived += socketTransport.socket_DataReceived;
                return socketTransport;
            }
        }

        private WebSocketTransport( IMessageSerializer serializer )
        {
            this.serializer = serializer;
        }

        private WebSocket socket;
        private bool channelBinaryMode;
        private IMessageSerializer serializer;
        private static readonly Dictionary<WebSocketState, TransportState> stateDict = new Dictionary<WebSocketState, TransportState>()
        {
            { WebSocketState.None, TransportState.Initialized },
            { WebSocketState.Connecting, TransportState.Connecting },
            { WebSocketState.Open, TransportState.Connected },
            { WebSocketState.Closing, TransportState.Closing },
            { WebSocketState.Closed, TransportState.Closed }
        };

        public string Host { get; private set; }

        public TransportState State
        {
            get
            {
                if( this.socket == null )
                {
                    return TransportState.Initialized;
                }
                return stateDict[ this.socket.State ];
            }
        }

        public ITransportListener Listener { get; set; }

        public void Connect()
        {
            this.socket.Open();
        }

        public void Close()
        {
            if( this.socket == null )
            {
                return;
            }
            this.socket.Close();
        }

        public void Abort( string reason )
        {
            this.socket.Close( reason );
        }

        public void Send( ProtocolMessage message )
        {
            object serializedMessage = this.serializer.SerializeProtocolMessage(message);

            if( this.channelBinaryMode )
            {
                byte[] data = (byte[])serializedMessage;
                this.socket.Send( data, 0, data.Length );
            }
            else
            {
                this.socket.Send( (string)serializedMessage );
            }
        }

        /// <summary>Convert names+values from WebHeaderCollection into HTTP GET request arguments</summary>
        static void setQuery( UriBuilder ub, WebHeaderCollection q )
        {
            StringBuilder sb = new StringBuilder();
            for( int i = 0; i < q.Count; i++ )
            {
                string key = q.GetKey(i);
                string val = q.Get(i);

                if( String.IsNullOrEmpty( key ) )
                    continue;
                if( sb.Length > 0 )
                    sb.Append( '&' );
                sb.Append( WebUtility.UrlEncode( key ) );
                sb.Append( '=' );
                sb.Append( WebUtility.UrlEncode( val ) );
            }
            ub.Query = sb.ToString();
        }

        private static WebSocket CreateSocket( TransportParams parameters )
        {
            bool isTls = parameters.Options.Tls;
            string wsScheme = isTls ? "wss://" : "ws://";
            WebHeaderCollection queryCollection = new WebHeaderCollection();
            parameters.StoreParams( queryCollection );

            UriBuilder uriBuilder = new UriBuilder(wsScheme, parameters.Host, parameters.Port);
            setQuery( uriBuilder, queryCollection );
            WebSocket socket = new WebSocket( uriBuilder.ToString(), "", WebSocketVersion.Rfc6455 );
            return socket;
        }

        private void socket_Opened( object sender, EventArgs e )
        {
            if( this.Listener != null )
            {
                this.Listener.OnTransportConnected();
            }
        }

        private void socket_Closed( object sender, EventArgs e )
        {
            if( this.Listener != null )
            {
                this.Listener.OnTransportDisconnected();
            }
        }

        private void socket_Error( object sender, SuperSocket.ClientEngine.ErrorEventArgs e )
        {
            if( this.Listener != null )
            {
                this.Listener.OnTransportError( e.Exception );
            }
        }

        private void socket_MessageReceived( object sender, MessageReceivedEventArgs e )
        {
            if( this.Listener != null )
            {
                ProtocolMessage message = this.serializer.DeserializeProtocolMessage(e.Message);
                this.Listener.OnTransportMessageReceived( message );
            }
        }

        private void socket_DataReceived( object sender, DataReceivedEventArgs e )
        {
            if( this.Listener != null )
            {
                ProtocolMessage message = this.serializer.DeserializeProtocolMessage(e.Data);
                this.Listener.OnTransportMessageReceived( message );
            }
        }
    }
}