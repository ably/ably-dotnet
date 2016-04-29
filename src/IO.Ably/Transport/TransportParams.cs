using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IO.Ably.Transport
{ 
    public class TransportParams
    {    
        public string Host { get; private set; }
        public bool Tls { get; private set; }
        public string[] FallbackHosts { get; private set; }
        public int Port { get; private set; }
        public string ConnectionKey { get; private set; }
        public long? ConnectionSerial { get; set; }
        public bool UseBinaryProtocol { get; private set; }

        //TODO: Look at inconsisten protection levels
        internal AuthMethod AuthMethod { get; private set; }
        public string AuthValue { get; private set; } //either key or token
        public string RecoverValue { get; private set; }
        public string ClientId { get; private set; }
        public bool EchoMessages { get; private set; }

        private TransportParams()
        {
            
        }

        internal static async Task<TransportParams> Create(IAuthCommands auth, ClientOptions options, string connectionKey = null, long? connectionSerial = null)
        {
            var result = new TransportParams();
            result.Host = options.FullRealtimeHost();
            result.Tls = options.Tls;
            result.Port = options.Tls ? options.TlsPort : options.Port;
            result.ClientId = options.GetClientId();
            result.AuthMethod = auth.AuthMethod;
            if (result.AuthMethod == AuthMethod.Basic)
            {
                result.AuthValue = options.Key;
            }
            else
            {
                try
                {
                    var token = await auth.GetCurrentValidTokenAndRenewIfNecessary();
                    if (token == null)
                        throw new AblyException("There is no valid token. Can't authenticate", 40100, HttpStatusCode.Unauthorized);

                    result.AuthValue = token.Token;
                }
                catch (AblyException ex)
                {
                    throw;
                }
                
            }
            result.ConnectionKey = connectionKey;
            result.ConnectionSerial = connectionSerial;
            result.EchoMessages = options.EchoMessages;
            result.FallbackHosts = Defaults.FallbackHosts;
            result.UseBinaryProtocol = options.UseBinaryProtocol;
            result.RecoverValue = options.Recover;

            return result;

        }

        //Add logic for random fallback hosts
        public Uri GetUri()
        {
            var wsScheme = Tls ? "wss://" : "ws://";
            var uriBuilder = new UriBuilder(wsScheme, Host, Port);
            uriBuilder.Query = GetParams().ToQueryString();
            return uriBuilder.Uri;
        }


        public Dictionary<string, string> GetParams()
        {
           //TODO: Write some tests 
           var result = new Dictionary<string, string>();

            if (AuthMethod == AuthMethod.Basic)
            {
                result["key"] = AuthValue;
            }
            else
            {
                result["accessToken"] = AuthValue;
            }

            result["v"] = Defaults.ProtocolVersion;
            //Url encode all the params at the time of creating the query string
            result["format"] = UseBinaryProtocol ? "msgpack" : "json";
            result["echo"] = EchoMessages.ToString().ToLower();

            if (ConnectionKey.IsNotEmpty())
            {
                result["resume"] = ConnectionKey;
                if (ConnectionSerial.HasValue)
                {
                    result["connection_serial"] = ConnectionSerial.Value.ToString();
                }
            }
            else if (RecoverValue.IsNotEmpty())
            {
                var pattern = new Regex(@"^([\w\-]+):(\-?\w+)$");
                var match = pattern.Match(RecoverValue);
                if (match.Success)
                {
                    result["recover"] = match.Groups[1].Value;
                    result["connection_serial"] = match.Groups[2].Value;
                }
            }

            if (ClientId.IsNotEmpty())
            {
                result["clientId"] = ClientId;
            }

            return result;
        }
    }
}