using IO.Ably.Types;
using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Net;

namespace IO.Ably.Transport
{
    public enum Mode
    {
        Clean,
        Resume,
        Recover
    }

    public class TransportParams
    {
        public TransportParams(AblyRealtimeOptions options)
        {
            this.Options = options;
        }

        public AblyRealtimeOptions Options { get; set; }
        public string Host { get; set; }
        public string[] FallbackHosts { get; set; }
        public int Port { get; set; }
        public string ConnectionKey { get; set; }
        public string ConnectionSerial { get; set; }
        public Mode Mode { get; set; }

        public void StoreParams( WebHeaderCollection collection )
        {
            // auth
            if (Options.Method == AuthMethod.Basic)
            {
                collection["key"] = WebUtility.UrlEncode(Options.Key);
            }
            else
            {
                collection["access_token"] = WebUtility.UrlEncode(Options.Token);
            }

            // connection
            if (Options.UseBinaryProtocol)
            {
                collection["format"] = "msgpack";
            }
            if (!Options.EchoMessages)
            {
                collection["echo"] = "false";
            }

            // recovery
            if (!string.IsNullOrEmpty(ConnectionKey))
            {
                Mode = Mode.Resume;
                collection["resume"] = ConnectionKey;
                if (!string.IsNullOrEmpty(ConnectionSerial))
                {
                    collection["connection_serial"] = ConnectionSerial;
                }
            }
            else if (!string.IsNullOrEmpty(Options.Recover))
            {
                Mode = Mode.Recover;
                Regex pattern = new Regex(@"^([\w\-]+):(\-?\w+)$");
                Match match = pattern.Match(Options.Recover);
                if (match.Success)
                {
                    collection["recover"] = match.Groups[1].Value;
                    collection["connection_serial"] = match.Groups[2].Value;
                }
            }

            if (!string.IsNullOrEmpty(Options.ClientId))
            {
                collection["client_id"] = Options.ClientId;
            }
        }
    }
}
