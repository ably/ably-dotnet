using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using MsgPack;
using Nito.AsyncEx;

namespace IO.Ably.Transport
{
    internal class MsWebSocketTransport : ITransport
    {
        internal readonly AsyncContextThread WebSocketThread = new AsyncContextThread();
        private MsWebSocketConnection _socket;

        protected MsWebSocketTransport(TransportParams parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters), "Null parameters are not allowed");

            BinaryProtocol = parameters.UseBinaryProtocol;
            WebSocketUri = parameters.GetUri();
        }

        public bool BinaryProtocol { get; }
        public Uri WebSocketUri { get; }

        public TransportState State { get; set; } = TransportState.Initialized;

        public ITransportListener Listener { get; set; }

        public void Connect()
        {
            if (_socket == null)
            {
                _socket = CreateSocket(WebSocketUri);
                AttachEvents();
            }

            WebSocketThread.Factory.StartNew(ConnectAndStartListening);
        }

        private async Task ConnectAndStartListening()
        {
            try
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Connecting socket");
                }
                Listener?.OnTransportEvent(TransportState.Connecting);
                
                await _socket.StartConnectionAsync();

                if (Logger.IsDebug) Logger.Debug("Socket connected");
                Listener?.OnTransportEvent(TransportState.Connected);

                await _socket.Receive(HandleMessageReceived);
            }
            catch (Exception ex)
            {
                if (Logger.IsDebug) Logger.Debug("Socket couldn't connect. Error: " + ex.Message);
                Listener?.OnTransportEvent(TransportState.Connecting, ex);
            }
        }

        private void HandleMessageReceived(RealtimeTransportData data)
        {
            if (data.IsBinary)
            {
                if (Logger.IsDebug)
                {
                    try
                    {
                        var message = MsgPackHelper.Deserialise(data.Data, typeof(MessagePackObject)).ToString();
                        Logger.Debug("Websocket data message received. Raw: " + message);
                    }
                    catch (Exception)
                    {
                        Logger.Debug("Error parsing message as MsgPack.");
                    }
                }

                Listener?.OnTransportDataReceived(data);
            }
            else
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Websocket message received. Raw: " + data.Text);
                }

                Listener?.OnTransportDataReceived(data);
            }

        }

        public void Close(bool suppressClosedEvent = true)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Closing socket. Current socket is " + (_socket == null ? "null" : "not null"));
            }

            if (_socket != null)
            {
                if (suppressClosedEvent)
                    DetachEvents();

                WebSocketThread.Factory.StartNew(_socket.StopConnectionAsync);
            }
        }

        public void Send(RealtimeTransportData data)
        {
            if (BinaryProtocol)
            {
                WebSocketThread.Factory.StartNew(() => _socket.SendData(data.Data))
                ;
            }
            else
            {
                WebSocketThread.Factory.StartNew(() => _socket.SendText(data.Text));
            }
        }

        private MsWebSocketConnection CreateSocket(Uri uri)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Connecting to web socket on url: " + uri);
            }

            return new MsWebSocketConnection(uri);
        }

        private void AttachEvents()
        {
            _socket?.SubscribeToEvents(HandleStateChange);
        }

        private void HandleStateChange(MsWebSocketConnection.ConnectionState state, Exception error)
        {
            if(Logger.IsDebug) Logger.Debug($"Transport State: {state}. Error is {error?.Message ?? "empty"}" );
            switch (state)
            {
                case MsWebSocketConnection.ConnectionState.Connecting:
                    Listener?.OnTransportEvent(TransportState.Connecting, error);
                    State = TransportState.Connecting;
                    break;
                case MsWebSocketConnection.ConnectionState.Connected:
                    Listener?.OnTransportEvent(TransportState.Connected, error);
                    State = TransportState.Connected;
                    break;
                case MsWebSocketConnection.ConnectionState.Error:
                    Listener?.OnTransportEvent(TransportState.Closing, error);
                    DisposeSocketConnection();
                    break;
                case MsWebSocketConnection.ConnectionState.Closing:
                    State = TransportState.Closing;
                    Listener?.OnTransportEvent(TransportState.Closing, error);
                    break;
                case MsWebSocketConnection.ConnectionState.Closed:
                    State =  TransportState.Closed;
                    Listener?.OnTransportEvent(TransportState.Closed, error);
                    DisposeSocketConnection();
                    break;
                default:
                    throw new Exception("Unrecognised HandleState");
            }
        }

        private void DisposeSocketConnection()
        {
            DetachEvents();
            _socket?.Dispose();
            _socket = null;
        }

        private void DetachEvents()
        {
            if (_socket != null)
            {
                try
                {
                    _socket.ClearStateHandler();
                }
                catch (Exception ex)
                {
                    Logger.Warning("Error while detaching events handlers. Error: {0}", ex.Message);
                }
            }
        }

        public class TransportFactory : ITransportFactory
        {
            public ITransport CreateTransport(TransportParams parameters)
            {
                return new MsWebSocketTransport(parameters);
            }
        }
    }
}
