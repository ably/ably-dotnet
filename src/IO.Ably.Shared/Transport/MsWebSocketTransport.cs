using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.Realtime;

namespace IO.Ably.Transport
{
    internal class MsWebSocketTransport : ITransport
    {
        private bool _disposed = false;

        internal ILogger Logger { get; private set; }

        internal MsWebSocketConnection _socket;

        protected MsWebSocketTransport(TransportParams parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "Null parameters are not allowed");
            }

            BinaryProtocol = parameters.UseBinaryProtocol;
            WebSocketUri = parameters.GetUri();
            Logger = parameters.Logger ?? DefaultLogger.LoggerInstance;
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

            Task.Run(ConnectAndStartListening).ConfigureAwait(false);
        }

        private async Task ConnectAndStartListening()
        {
            try
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Connecting socket");
                }

                await _socket.StartConnectionAsync();

                if (Logger.IsDebug)
                {
                    Logger.Debug("Socket connected");
                }

                await _socket.Receive(HandleMessageReceived);
            }
            catch (Exception ex)
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Socket couldn't connect. Error: " + ex.Message);
                }

                Listener?.OnTransportEvent(TransportState.Closed, ex);
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
#if MSGPACK
                        var message = MsgPackHelper.DeserialiseMsgPackObject(data.Data).ToString();
                        Logger.Debug("Websocket data message received. Raw: " + message);
#endif
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
                {
                    DetachEvents();
                }

                TaskUtils.RunInBackground(async () => await _socket.StopConnectionAsync(), e => Logger.Warning(e.Message));
            }
        }

        public void Send(RealtimeTransportData data)
        {
            if (BinaryProtocol)
            {
                _socket?.SendData(data.Data);
            }
            else
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Sending Text: " + data.Text);
                }

                _socket?.SendText(data.Text);
            }
        }

        private MsWebSocketConnection CreateSocket(Uri uri)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Connecting to web socket on url: " + uri);
            }

            return new MsWebSocketConnection(uri, Logger);
        }

        private void AttachEvents()
        {
            _socket?.SubscribeToEvents(HandleStateChange);
        }

        private void HandleStateChange(MsWebSocketConnection.ConnectionState state, Exception error)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"Transport State: {state}. Error is {error?.Message ?? "empty"}. {error?.StackTrace}");
            }

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
                    if (State != TransportState.Closing && State != TransportState.Closed)
                    {
                        Listener?.OnTransportEvent(TransportState.Closing, error);
                    }

                    DisposeSocketConnection();
                    break;
                case MsWebSocketConnection.ConnectionState.Closing:
                    State = TransportState.Closing;
                    Listener?.OnTransportEvent(TransportState.Closing, error);
                    break;
                case MsWebSocketConnection.ConnectionState.Closed:
                    State = TransportState.Closed;
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
            try
            {
                _socket?.Dispose();
            }
            catch (Exception e)
            {
                Logger.Warning("Error while disposing socket. Nothing to worry about. Message: " + e.Message);
            }
            finally
            {
                _socket = null;
            }
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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeSocketConnection();
                Listener = null;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);

            // Typically we would call GC.SuppressFinalize(this)
            // at this point in the Dispose pattern
            // to suppress an expensive GC cycle
            // But disposing of the Transport should not be frequent
            // and based on profiling this speeds up the release of objects
            // and reduces memory bloat considerably
        }

        ~MsWebSocketTransport()
        {
            Dispose(false);
        }
    }
}
