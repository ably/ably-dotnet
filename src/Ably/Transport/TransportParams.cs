using Ably.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public TransportParams(AblyOptions options)
        {
            this.Options = options;
        }

        public AblyOptions Options { get; set; }
        public string Host { get; set; }
        public string[] FallbackHosts { get; set; }
        public int Port { get; set; }
        public string ConnectionKey { get; set; }
        public string ConnectionSerial { get; set; }
        public Mode Mode { get; set; }

        public void StoreParams(NameValueCollection collection)
        {
            // auth
            collection["key_id"] = Options.KeyId;
            collection["key_value"] = Options.KeyValue;

            // connection
            if (!Options.UseTextProtocol)
            {
                collection["format"] = "msgpack";
            }
            if (Options.EchoMessages)
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
