using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Ably
{
    public class AblyRequest
    {
        private ChannelOptions _channelOptions;

        public AblyRequest(string path, HttpMethod method, Protocol protocol = Protocol.MsgPack)
        {
            Url = path;
            QueryParameters = new Dictionary<string, string>();
            Headers = new Dictionary<string, string>();
            PostParameters = new Dictionary<string, string>();
            Method = method;
            Protocol = protocol;
            ChannelOptions = new ChannelOptions();
            RequestBody = new byte[] {};
        }

        public string Url { get; private set; }
        public HttpMethod Method { get; private set; }

        public Dictionary<string, string> Headers { get; private set; }
        public Dictionary<string, string> QueryParameters { get; private set; }
        
        public void AddQueryParameters(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            foreach (var keyValuePair in parameters)
            {
                QueryParameters.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        
        public Protocol Protocol { get; set; }
        public object PostData { get; set; }

        public ChannelOptions ChannelOptions
        {
            get { return _channelOptions; }
            set { _channelOptions = value ?? new ChannelOptions(); }
        }

        public Dictionary<string, string> PostParameters { get; set; }
        public byte[] RequestBody { get; set; }
        public bool SkipAuthentication { get; set; }
    }
}
