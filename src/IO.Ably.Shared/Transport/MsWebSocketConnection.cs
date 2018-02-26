using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using IO.Ably;
using IO.Ably.Realtime;

namespace IO.Ably.Transport
{
    internal class MsWebSocketConnection : IDisposable
    {
        public enum ConnectionState
        {
            Connecting,
            Connected,
            Error,
            Closing,
            Closed
        }

        internal ILogger Logger { get; private set; }

        private readonly Uri _uri;
        private Action<ConnectionState, Exception> _handler;
        private readonly BlockingCollection<Tuple<ArraySegment<byte>, WebSocketMessageType>> _sendQueue
            = new BlockingCollection<Tuple<ArraySegment<byte>, WebSocketMessageType>>();

        public string ConnectionId { get; set; }

        internal ClientWebSocket ClientWebSocket { get; set; }

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MsWebSocketConnection(Uri uri, ILogger logger)
        {
            Logger = logger;
            _uri = uri;
            ClientWebSocket = new ClientWebSocket();
        }

        public void SubscribeToEvents(Action<ConnectionState, Exception> handler) => _handler = handler;

        public void ClearStateHandler() => _handler = null;

        public async Task StartConnectionAsync()
        {
            _tokenSource = new CancellationTokenSource();
            _handler?.Invoke(ConnectionState.Connecting, null);
            try
            {
                await ClientWebSocket.ConnectAsync(_uri, CancellationToken.None).ConfigureAwait(false);
                StartSenderQueueConsumer();
                _handler?.Invoke(ConnectionState.Connected, null);
            }
            catch (Exception ex)
            {
                _handler?.Invoke(ConnectionState.Error, ex);
            }
        }

        private void StartSenderQueueConsumer()
        {
            Task.Run(
                async () =>
                {
                    foreach (var tuple in _sendQueue.GetConsumingEnumerable())
                    {
                        await Send(tuple.Item1, tuple.Item2, _tokenSource.Token);
                    }
                }, _tokenSource.Token).ConfigureAwait(false);
        }

        public async Task StopConnectionAsync()
        {
            _tokenSource.Cancel();
            _handler?.Invoke(ConnectionState.Closing, null);
            try
            {
                if (ClientWebSocket.CloseStatus.HasValue)
                {
                    Logger.Debug("Closing websocket. Close status: " +
                                 Enum.GetName(typeof(WebSocketCloseStatus), ClientWebSocket.CloseStatus) + ", Description: " + ClientWebSocket.CloseStatusDescription);
                }

                if (ClientWebSocket?.State != WebSocketState.Closed)
                {
                    await
                    ClientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                        .ConfigureAwait(false);
                }

                _handler?.Invoke(ConnectionState.Closed, null);
            }
            catch (Exception ex)
            {
                _handler?.Invoke(ConnectionState.Closed, ex);
            }
        }

        public void SendText(string message)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Sending text");
            }

            var bytes = new ArraySegment<byte>(message.GetBytes());
            _sendQueue.TryAdd(Tuple.Create(bytes, WebSocketMessageType.Text), 1000, _tokenSource.Token);
        }

        public void SendData(byte[] data)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Sending binary data");
            }

            var bytes = new ArraySegment<byte>(data);
            _sendQueue.TryAdd(Tuple.Create(bytes, WebSocketMessageType.Binary), 1000, _tokenSource.Token);
        }

        private Task Send(ArraySegment<byte> data, WebSocketMessageType type, CancellationToken token)
        {
            try
            {
                if (ClientWebSocket.State == WebSocketState.Open)
                {
                    var sendTask = ClientWebSocket.SendAsync(data, type, true, token);
                    sendTask.ConfigureAwait(false);
                    return sendTask;
                }

                Logger.Warning($"Trying to send message of type {type} when the socket is {ClientWebSocket.State}. Ack for this message will fail shortly.");

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _handler?.Invoke(ConnectionState.Error, ex);
            }

            return Task.FromResult(false);
        }

        public async Task Receive(Action<RealtimeTransportData> handleMessage)
        {
            while (ClientWebSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var buffer = new ArraySegment<byte>(new byte[1024 * 16]); // Default receive buffer size
                    WebSocketReceiveResult result = null;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            result = await ClientWebSocket.ReceiveAsync(buffer, _tokenSource.Token);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                break;
                            }

                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);

                        ms.Seek(0, SeekOrigin.Begin);

                        if (Logger.IsDebug)
                        {
                            Logger.Debug("Recieving message with type: " + result.MessageType);
                        }

                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                var text = ms.ToArray().GetText();
                                handleMessage?.Invoke(new RealtimeTransportData(text));
                                break;
                            case WebSocketMessageType.Binary:
                                handleMessage?.Invoke(new RealtimeTransportData(ms.ToArray()));
                                break;
                        }
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await StopConnectionAsync();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _handler?.Invoke(ConnectionState.Error, ex);
                    break;
                }
            }
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _sendQueue.Dispose();
            ClientWebSocket?.Dispose();
        }
    }
}
