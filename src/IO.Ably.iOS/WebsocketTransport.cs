using System;
using System.Collections.Generic;
using IO.Ably.Realtime;
using WebSocket4Net;
using IO.Ably.Transport;
using SuperSocket.ClientEngine;

namespace IO.Ably
{
    public class WebSocketTransport : ITransport
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

        protected WebSocketTransport(TransportParams parameters)
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

            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug("Connecting socket");
            }
            _socket.Open();
        }

        public void Close(bool suppressClosedEvent = true)
        {
            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug("Closing socket. Current socket is " + (_socket == null ? "null" : "not null"));
            }

            if (_socket != null)
            {
                if (suppressClosedEvent)
                    DetachEvents();

                try
                {
                    _socket.Close();
                }
                catch (Exception ex)
                {
                    DefaultLogger.Warning(
                        $"Error while closing the socket transport. suppressClosedEvent={suppressClosedEvent}. Error message: {ex.Message}");
                }
            }
        }

        public void Send(RealtimeTransportData data)
        {
            if (DefaultLogger.IsDebug) DefaultLogger.Debug($"Transport state ({_socket?.State}): Sending message. Action: {data.Original.Action} - " + data.Explain());
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
            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug("Connecting to web socket on url: " + uri);
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
                    DefaultLogger.Warning("Error while detaching events handlers. Error: {0}", ex.Message);
                }
            }
        }

        private void socket_Opened(object sender, EventArgs e)
        {
            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug("Websocket opened!");
            }

            Listener?.OnTransportEvent(State);
        }

        private void socket_Closed(object sender, EventArgs e)
        {
            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug("Websocket closed!");
            }
            Listener?.OnTransportEvent(State);


            DetachEvents();
            _socket = null;
        }

        private void socket_Error(object sender, ErrorEventArgs e)
        {
            DefaultLogger.Error("Websocket error!", e.Exception);
            Listener?.OnTransportEvent(State, e.Exception);
        }

        private void socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug("Websocket message received. Raw: " + e.Message);
            }

            Listener?.OnTransportDataReceived(new RealtimeTransportData(e.Message));
        }

        private void socket_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (DefaultLogger.IsDebug)
            {
                try
                {
                    var message = MsgPackHelper.DeserialiseMsgPackObject(e.Data).ToString();
                    DefaultLogger.Debug("Websocket data message received. Raw: " + message);
                }
                catch (Exception)
                {
                    DefaultLogger.Debug("Error parsing message as MsgPack.");
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

        public void Dispose()
        {
            if (_socket != null)
            {
                Close(true);
                _socket?.Dispose();
                _socket = null;
            }
        }
    }


}
