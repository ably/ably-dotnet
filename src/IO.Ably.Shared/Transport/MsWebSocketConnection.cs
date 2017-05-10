using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably.Transport
{
    public class MsWebSocketConnection : IDisposable
    {
        public enum ConnectionState
        {
            Connecting,
            Connected,
            Error,
            Closing,
            Closed
        }

        private readonly Uri _uri;
        private Action<ConnectionState, Exception> _handler;
        private BlockingCollection<ValueTuple<ArraySegment<byte>, WebSocketMessageType>> _sendQueue = new BlockingCollection<ValueTuple<ArraySegment<byte>, WebSocketMessageType>>();

        public string ConnectionId { get; set; }

        private ClientWebSocket _clientWebSocket { get; set; }
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MsWebSocketConnection(Uri uri)
        {
            _uri = uri;
            _clientWebSocket = new ClientWebSocket();
        }

        public void SubscribeToEvents(Action<ConnectionState, Exception> handler)
        {
            _handler = handler;
        }

        public void ClearStateHandler()
        {
            _handler = null;
        }

        public async Task StartConnectionAsync()
        {
            _handler?.Invoke(ConnectionState.Connecting, null);
            try
            {
                await _clientWebSocket.ConnectAsync(_uri, CancellationToken.None).ConfigureAwait(false);
                //initialise sender
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
            Task.Run((async () =>
            {
                foreach ((ArraySegment<byte> bytes, WebSocketMessageType type) in _sendQueue.GetConsumingEnumerable())
                {
                    await Send(bytes, type, _tokenSource.Token);
                }
            }), _tokenSource.Token);
        }

        public async Task StopConnectionAsync()
        {
            _tokenSource.Cancel();
            _handler?.Invoke(ConnectionState.Closing, null);
            try
            {
                await
                    _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
                        .ConfigureAwait(false);
                _handler?.Invoke(ConnectionState.Closed, null);
            }
            catch (Exception ex)
            {
                _handler?.Invoke(ConnectionState.Error, ex);
            }
        }

        public void SendText(string message)
        {
            if(Logger.IsDebug) Logger.Debug("Sending text");

            var bytes = new ArraySegment<byte>(message.GetBytes());
            _sendQueue.TryAdd((bytes, WebSocketMessageType.Text), 1000, _tokenSource.Token);
        }

        public void SendData(byte[] data)
        {
            if (Logger.IsDebug) Logger.Debug("Sending binary data");

            var bytes = new ArraySegment<byte>(data);
            _sendQueue.TryAdd((bytes, WebSocketMessageType.Binary), 1000, _tokenSource.Token);
        }

        private Task Send(ArraySegment<byte> data, WebSocketMessageType type, CancellationToken token)
        {
            var sendTask = _clientWebSocket.SendAsync(data, type, true, token);
            sendTask.ConfigureAwait(false);
            return sendTask;
        }

        public async Task Receive(Action<RealtimeTransportData> handleMessage)
        {
            while (_clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
                    WebSocketReceiveResult result = null;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            result = await _clientWebSocket.ReceiveAsync(buffer, _tokenSource.Token).ConfigureAwait(false);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);

                        ms.Seek(0, SeekOrigin.Begin);

                        if(Logger.IsDebug) Logger.Debug("Recieving message with type: " + result.MessageType);
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
            _clientWebSocket?.Dispose();
        }
    }
}