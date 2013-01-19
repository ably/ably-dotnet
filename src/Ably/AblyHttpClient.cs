using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;

namespace Ably
{
    public class AblyHttpClient : IAblyHttpClient
    {
        private readonly string _Host;
        private readonly int? _Port;
        private readonly bool _IsSecure;
        private readonly string _basePath;

        


        public AblyHttpClient(string appId, string host) : this(appId, host, null, true) { }

        public AblyHttpClient(string appId, string host, int? port = null, bool isSecure = true)
        {
            _basePath = "/apps/" + appId;
            _IsSecure = isSecure;
            _Port = port;
            _Host = host;
        }

        public AblyResponse Get(AblyRequest request)
        {
            var client = new HttpClient();
            
            throw new NotImplementedException();
        }

        public AblyResponse Delete(AblyResponse request)
        {
            throw new NotImplementedException();
        }

        public AblyResponse Post(AblyResponse request)
        {
            throw new NotImplementedException();
        }


        
    }
}
