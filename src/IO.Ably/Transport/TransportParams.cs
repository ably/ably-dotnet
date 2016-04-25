using System.Net;
using System.Text.RegularExpressions;

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
        public TransportParams(ClientOptions options)
        {
            Options = options;
        }

        public ClientOptions Options { get; set; }
        public string Host { get; set; }
        public string[] FallbackHosts { get; set; }
        public int Port { get; set; }
        public string ConnectionKey { get; set; }
        public string ConnectionSerial { get; set; }
        public Mode Mode { get; set; }


        //TODO: Move so this is handled by the Auth object and ensures all the rules about renewing are followed
        public void StoreParams(WebHeaderCollection collection)
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
            if (ConnectionKey.IsNotEmpty())
            {
                Mode = Mode.Resume;
                collection["resume"] = ConnectionKey;
                if (ConnectionSerial.IsNotEmpty())
                {
                    collection["connection_serial"] = ConnectionSerial;
                }
            }
            else if (Options.Recover.IsNotEmpty())
            {
                Mode = Mode.Recover;
                var pattern = new Regex(@"^([\w\-]+):(\-?\w+)$");
                var match = pattern.Match(Options.Recover);
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