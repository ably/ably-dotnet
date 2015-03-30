using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Ably
{
    public class AblyRequest
    {
        public AblyRequest(string path, HttpMethod method)
        {
            Url = path;
            QueryParameters = new Dictionary<string, string>();
            Headers = new Dictionary<string, string>();
            PostParameters = new Dictionary<string, string>();
            Method = method;
            UseTextProtocol = true;
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
        
        public bool UseTextProtocol { get; set; }
        public object PostData { get; set; }
        public bool Encrypted { get; set; }
        public CipherParams CipherParams { get; set; }
        public Dictionary<string, string> PostParameters { get; set; }
        public bool SkipAuthentication { get; set; }
    }
}
