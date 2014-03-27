using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient;

namespace Ably.Realtime
{
    class WebSocketTransport : ITransport
    {
        private readonly TransportParams _config;
        private readonly ConnectionManager _connectionManager;
        private bool _channelBinaryMode;
        private Client _client;

        public WebSocketTransport(TransportParams config, ConnectionManager connectionManager)
        {
            _config = config;
            _connectionManager = connectionManager;
            _channelBinaryMode = !config.Options.UseTextProtocol;

        }

        public void Connect(IConnectListener connectListener)
        {
            string wsUri = "";
            try
            {
                bool tls = _config.Options.Tls;
                string wsScheme = tls ? "wss://" : "ws://";
                wsUri = wsScheme + _config.Host + ':' + _config.Port + "/";
                _client.Connect();
                var requestParams = _config.GetConnectParams(_config.Options.AuthParams);
                wsUri += GetQueryString(requestParams);
                _client = new Client(wsUri);
            }
            catch (AblyException ablyException)
            {
                Logger.Current.Error("Unexpected exception attempting connection; wsUri = " + wsUri, ablyException);
                connectListener.OnTransportUnavailable(this, _config, ablyException.ErrorInfo);
            }
            catch (Exception ex)
            {
                Logger.Current.Error("Unexpected exception attempting connection; wsUri = " + wsUri, ex);
                connectListener.OnTransportUnavailable(this, _config, new AblyException(ex).ErrorInfo);
            }
        }

        private string GetQueryString(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            string query = string.Join("&", queryParams.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            if (query.IsNotEmpty())
                return "?" + query;
            return string.Empty;
        }

        public void Close(bool sendDisconnect)
        {
            throw new NotImplementedException();
        }

        public void Abort(ErrorInfo reason)
        {
            throw new NotImplementedException();
        }

        public void Send(ProtocolMessage msg)
        {
            throw new NotImplementedException();
        }

        public string GetHost()
        {
            throw new NotImplementedException();
        }
    }
}
