using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using IO.Ably.Transport;
using IO.Ably.Types;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace IO.Ably.Realtime
{
    internal class WebSocketTransport : ITransport
    {
        private static readonly Dictionary<WebSocketState, TransportState> stateDict = new Dictionary
            <WebSocketState, TransportState>
        {
            {WebSocketState.None, TransportState.Initialized},
            {WebSocketState.Connecting, TransportState.Connecting},
            {WebSocketState.Open, TransportState.Connected},
            {WebSocketState.Closing, TransportState.Closing},
            {WebSocketState.Closed, TransportState.Closed}
        };

        private readonly IMessageSerializer serializer;

        private bool channelBinaryMode;

        private WebSocket socket;

        private WebSocketTransport(IMessageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public string Host { get; private set; }

        public TransportState State
        {
            get
            {
                if (socket == null)
                {
                    return TransportState.Initialized;
                }
                return stateDict[socket.State];
            }
        }

        public ITransportListener Listener { get; set; }

        public void Connect()
        {
            socket.Open();
        }

        public void Close()
        {
            if (socket == null)
            {
                return;
            }
            socket.Close();
        }

        public void Abort(string reason)
        {
            socket.Close(reason);
        }

        public void Send(ProtocolMessage message)
        {
            var serializedMessage = serializer.SerializeProtocolMessage(message);

            if (channelBinaryMode)
            {
                var data = (byte[]) serializedMessage;
                socket.Send(data, 0, data.Length);
            }
            else
            {
                socket.Send((string) serializedMessage);
            }
        }

        /// <summary>Convert names+values from WebHeaderCollection into HTTP GET request arguments</summary>
        private static void setQuery(UriBuilder ub, WebHeaderCollection q)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < q.Count; i++)
            {
                var key = q.GetKey(i);
                var val = q.Get(i);

                if (string.IsNullOrEmpty(key))
                    continue;
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(WebUtility.UrlEncode(key));
                sb.Append('=');
                sb.Append(WebUtility.UrlEncode(val));
            }
            ub.Query = sb.ToString();
        }

        private static WebSocket CreateSocket(TransportParams parameters)
        {
            var isTls = parameters.Options.Tls;
            var wsScheme = isTls ? "wss://" : "ws://";
            var queryCollection = new WebHeaderCollection();
            parameters.StoreParams(queryCollection);

            var uriBuilder = new UriBuilder(wsScheme, parameters.Host, parameters.Port);
            setQuery(uriBuilder, queryCollection);
            var socket = new WebSocket(uriBuilder.ToString(), "", WebSocketVersion.Rfc6455);
            return socket;
        }

        private void socket_Opened(object sender, EventArgs e)
        {
            if (Listener != null)
            {
                Listener.OnTransportConnected();
            }
        }

        private void socket_Closed(object sender, EventArgs e)
        {
            if (Listener != null)
            {
                Listener.OnTransportDisconnected();
            }
        }

        private void socket_Error(object sender, ErrorEventArgs e)
        {
            if (Listener != null)
            {
                Listener.OnTransportError(e.Exception);
            }
        }

        private void socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (Listener != null)
            {
                var message = serializer.DeserializeProtocolMessage(e.Message);
                Listener.OnTransportMessageReceived(message);
            }
        }

        private void socket_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (Listener != null)
            {
                var message = serializer.DeserializeProtocolMessage(e.Data);
                Listener.OnTransportMessageReceived(message);
            }
        }

        public class WebSocketTransportFactory : ITransportFactory
        {
            public ITransport CreateTransport(TransportParams parameters)
            {
                IMessageSerializer serializer = null;
                if (parameters.Options.UseBinaryProtocol)
                {
                    serializer = new MsgPackMessageSerializer();
                }
                else
                {
                    serializer = new JsonMessageSerializer();
                }
                var socketTransport = new WebSocketTransport(serializer);
                socketTransport.Host = parameters.Host;
                socketTransport.channelBinaryMode = parameters.Options.UseBinaryProtocol;
                socketTransport.socket = CreateSocket(parameters);
                socketTransport.socket.Opened += socketTransport.socket_Opened;
                socketTransport.socket.Closed += socketTransport.socket_Closed;
                socketTransport.socket.Error += socketTransport.socket_Error;
                socketTransport.socket.MessageReceived += socketTransport.socket_MessageReceived;
                socketTransport.socket.DataReceived += socketTransport.socket_DataReceived;
                return socketTransport;
            }
        }
    }
}