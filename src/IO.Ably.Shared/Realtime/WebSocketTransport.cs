using System;
using System.Collections.Generic;
using System.Net.Configuration;
using System.Threading.Tasks;
using IO.Ably.Transport;
using MsgPack;
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


        private WebSocket _socket;

        private WebSocketTransport(TransportParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Null parameters are not allowed");
        
            BinaryProtocol = parameters.UseBinaryProtocol;
            WebSocketUri = parameters.GetUri();
        }

        public bool BinaryProtocol { get; }
        public Uri WebSocketUri { get; }

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
            if (_socket == null)
            {
                _socket = CreateSocket(WebSocketUri);
                AttachEvents();
            }
            
            if (Logger.IsDebug)
            {
                Logger.Debug("Connecting socket");
            }
            _socket.Open();
        }

        public void Close(bool suppressClosedEvent = true)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Closing socket. Current socket is " + (_socket == null ? "null" : "not null"));
            }
            if (_socket != null)
            {
                if(suppressClosedEvent)
                    DetachEvents();

                _socket.Close();

                
            }
        }

        public void Send(RealtimeTransportData data)
        {
            if (BinaryProtocol)
            {
                _socket.Send(data.Data, 0, data.Length);
            }
            else
            {
                _socket.Send(data.Text);
            }
        }

        private WebSocket CreateSocket(Uri uri)
        { 
            if (Logger.IsDebug)
            {
                Logger.Debug("Connecting to web socket on url: " + uri);
            }

            return new WebSocket(uri.ToString(), "", WebSocketVersion.Rfc6455);
        }

        private void AttachEvents()
        {
            if (_socket != null)
            {
                _socket.Opened += socket_Opened;
                _socket.Closed += socket_Closed;
                _socket.Error += socket_Error;
                _socket.MessageReceived += socket_MessageReceived; //For text messages
                _socket.DataReceived += socket_DataReceived; //For binary messages    
            }
        }

        private void DetachEvents()
        {
            if (_socket != null)
            {
                try
                {
                    _socket.Opened -= socket_Opened;
                    _socket.Closed -= socket_Closed;
                    _socket.Error -= socket_Error;
                    _socket.MessageReceived -= socket_MessageReceived; //For text messages
                    _socket.DataReceived -= socket_DataReceived; //For binary messages    
                }
                catch (Exception ex)
                {
                    Logger.Warning("Error while detaching events handlers. Error: {0}", ex.Message);   
                }
            }
        }

        private void socket_Opened(object sender, EventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket opened!");
            }

            Listener?.OnTransportEvent(State);
        }

        private void socket_Closed(object sender, EventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket closed!");
            }
            Listener?.OnTransportEvent(State);
            

            DetachEvents();
            _socket = null;
        }

        private void socket_Error(object sender, ErrorEventArgs e)
        {
            Logger.Error("Websocket error!", e.Exception);
            Listener?.OnTransportEvent(State, e.Exception);
        }

        private void socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Websocket message received. Raw: " + e.Message);
            }

            Listener?.OnTransportDataReceived(new RealtimeTransportData(e.Message));
        }

        private void socket_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (Logger.IsDebug)
            {
                try
                {
                   var message = MsgPackHelper.DeSerialise(e.Data, typeof(MessagePackObject)).ToString();
                   Logger.Debug("Websocket data message received. Raw: " + message);
                }
                catch (Exception)
                {
                    Logger.Debug("Error parsing message as MsgPack.");
                }
            }

            Listener?.OnTransportDataReceived(new RealtimeTransportData(e.Data));
        }

        public class WebSocketTransportFactory : ITransportFactory
        {
            public ITransport CreateTransport(TransportParams parameters)
            {
                return new WebSocketTransport(parameters);
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