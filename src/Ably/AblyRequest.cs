using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class AblyRequest
    {
        private string host = "rest.ably.io";

        public AblyRequest(string url)
        {
            Url = url;
            PostParameters = new Dictionary<string, string>();
            QueryParameters = new Dictionary<string, string>();
        }

        public string Url { get; private set; }

        public Dictionary<string, string> QueryParameters { get; private set; }
        public Dictionary<string, string> PostParameters { get; private set; }
    }
}
