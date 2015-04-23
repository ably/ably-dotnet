using Ably.Types;
using System;
using System.Collections.Specialized;
using System.Web;

namespace Ably.Transport
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

        public void StoreParams(NameValueCollection collection)
        {
            // auth
            collection["key"] = HttpUtility.UrlEncode(Options.Key);

            // connection
            if (Options.UseBinaryProtocol)
            {
                collection["format"] = "msgpack";
            }
            if (!Options.EchoMessages)
            {
                collection["echo"] = "false";
            }
            if (!string.IsNullOrEmpty(Options.ClientId))
            {
                collection["client_id"] = Options.ClientId;
            }
        }
    }
}
