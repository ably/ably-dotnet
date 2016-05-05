using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.Transport;
using IO.Ably.Types;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace IO.Ably.Realtime
{
    internal class WebSocketTransport : ITransport
    {
        private static readonly Dictionary<WebSocketState, TransportState> StateDict = new Dictionary
            <WebSocketState, TransportState>
        {
            {WebSocketState.None, TransportState.Initialized},
            {WebSocketState.Connecting, TransportState.Connecting},
            {WebSocketState.Open, TransportState.Connected},
            {WebSocketState.Closing, TransportState.Closing},
            {WebSocketState.Closed, TransportState.Closed}
        };

        private readonly IMessageSerializer _serializer;
        private readonly TransportParams _parameters;

        private WebSocket _socket;

        private WebSocketTransport(IMessageSerializer serializer, TransportParams parameters)
        {
            _serializer = serializer;
            _parameters = parameters;
        }

        public string Host => _parameters.Host;

        public TransportState State
        {
            get
            {
                if (_socket == null)
                {
                    return TransportState.Initialized;
                }
                return StateDict[_socket.State];
            }
        }

        public ITransportListener Listener { get; set; }

        public void Connect()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Connecting socket");
            }
            _socket.Open();
        }

        public void Close()
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Closing socket. Current socket is " + (_socket == null ? "null" : "not null"));
            }
            _socket?.Close();
        }

        public void Abort(string reason)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Aborting socket. Reason: " + reason);
            }
            _socket.Close(reason);
        }

        public void Send(ProtocolMessage message)
        {
            var serializedMessage = _serializer.SerializeProtocolMessage(message);

            if (_parameters.UseBinaryProtocol)
            {
                var data = (byte[]) serializedMessage;
                _socket.Send(data, 0, data.Length);
            }
            else
            {
                _socket.Send((string) serializedMessage);
            }
        }

        private Task CreateSocket()
        {
            if(_parameters == null)
                throw new ArgumentNullException(nameof(_parameters), "Null parameters are not allowed");

            var uri = _parameters.GetUri();
            if (Logger.IsDebug)
            {
                Logger.Debug("Connecting to web socket on url: " + uri);
            }
            _socket = new WebSocket(uri.ToString(), "", WebSocketVersion.Rfc6455);
            _socket.Opened += socket_Opened;
            _socket.Closed += socket_Closed;
            _socket.Error += socket_Error;
            _socket.MessageReceived += socket_MessageReceived; //For text messages
            _socket.DataReceived += socket_DataReceived; //For binary messages

            return Task.FromResult(true);
        }

        private void socket_Opened(object sender, EventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket opened!");
            }
            Listener?.OnTransportConnected();
        }

        private void socket_Closed(object sender, EventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket closed!");
            }
            Listener?.OnTransportDisconnected();
        }

        private void socket_Error(object sender, ErrorEventArgs e)
        {
            Logger.Error("Websocket error!", e.Exception);
            Listener?.OnTransportError(e.Exception);
        }

        private void socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket message received. Raw: " + e.Message);
            }

            if (Listener != null)
            {
                var message = _serializer.DeserializeProtocolMessage(e.Message);
                Listener.OnTransportMessageReceived(message);
            }
        }

        private void socket_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket data message received. Raw: " + e.Data.GetText());
            }

            if (Listener != null)
            {
                var message = _serializer.DeserializeProtocolMessage(e.Data);
                Listener.OnTransportMessageReceived(message);
            }
        }

        public class WebSocketTransportFactory : ITransportFactory
        {
            public async Task<ITransport> CreateTransport(TransportParams parameters)
            {
                IMessageSerializer serializer = null;
                if (parameters.UseBinaryProtocol)
                {
                    serializer = new MsgPackMessageSerializer();
                }
                else
                {
                    serializer = new JsonMessageSerializer();
                }
                var socketTransport = new WebSocketTransport(serializer, parameters);
                await socketTransport.CreateSocket();
                return socketTransport;
            }
        }
    }

    /* 
     * 
     * http://www.tomdupont.net/2015/12/websocket4net-extensions-openasync.html
     * Error handling logic. Look a bit later
     * public static class WebSocketExtensions
{
    public static async Task OpenAsync(
        this WebSocket webSocket,
        int retryCount = 5,
        CancellationToken cancelToken = default(CancellationToken))
    {
        var failCount = 0;
        var exceptions = new List<Exception>(retryCount);
 
        var openCompletionSource = new TaskCompletionSource<bool>();
        cancelToken.Register(() => openCompletionSource.TrySetCanceled());
 
        EventHandler openHandler = (s, e) => openCompletionSource.TrySetResult(true);
 
        EventHandler<ErrorEventArgs> errorHandler = (s, e) =>
        {
            if (exceptions.All(ex => ex.Message != e.Exception.Message))
            {
                exceptions.Add(e.Exception);
            }
        };
 
        EventHandler closeHandler = (s, e) =>
        {
            if (cancelToken.IsCancellationRequested)
            {
                openCompletionSource.TrySetCanceled();
            }
            else if (++failCount < retryCount)
            {
                webSocket.Open();
            }
            else
            {
                var exception = exceptions.Count == 1
                    ? exceptions.Single()
                    : new AggregateException(exceptions);
 
                var webSocketException = new WebSocketException(
                    "Unable to connect", 
                    exception);
 
                openCompletionSource.TrySetException(webSocketException);
            }
        };
 
        try
        {
            webSocket.Opened += openHandler;
            webSocket.Error += errorHandler;
            webSocket.Closed += closeHandler;
 
            webSocket.Open();
 
            await openCompletionSource.Task.ConfigureAwait(false);
        }
        finally
        {
            webSocket.Opened -= openHandler;
            webSocket.Error -= errorHandler;
            webSocket.Closed -= closeHandler;
        }
    }
     * 
     * 
     */
}