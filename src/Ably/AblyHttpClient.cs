using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Ably
{
    public class AblyHttpClient : IAblyHttpClient
    {
        private readonly string _Host;
        private readonly int? _Port;
        private readonly bool _IsSecure;
        private readonly string _basePath;

        static IDictionary<string, string> mimeTypes = new Dictionary<String, String>();

        static AblyHttpClient()
        {
            mimeTypes.Add("json", "application/json");
            mimeTypes.Add("xml", "application/xml");
            mimeTypes.Add("html", "text/html");
            mimeTypes.Add("binary", "application/x-thrift");
        }

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


        private static NameValueCollection GetDefaultHeaders(bool binary)
        {
            NameValueCollection headers = new NameValueCollection();
            if (binary)
            {
                headers.Add("Accept", "application/x-thrift,application/json");
            }
            else
            {
                headers.Add("Accept", "application/json");
            }
            return headers;
        }

        private static NameValueCollection GetDefaultPostHeaders(bool binary)
        {

            var headers = new NameValueCollection();
            if (binary)
            {
                headers.Add("Accept", "application/x-thrift,application/json");
                headers.Add("Content-Type", "application/x-thrift");
            }
            else
            {
                headers.Add("Accept", "application/json");
                headers.Add("Content-Type", "application/json");
            }
            return headers;
        }
    }
}
