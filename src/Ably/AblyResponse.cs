using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;

namespace Ably
{
    public enum ResponseType
    {
        Json,
        Thrift
    }

    public class AblyResponse
    {
        public NameValueCollection Headers { get; set; } 
        public ResponseType Type { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string JsonResult { get; set; }

        public AblyResponse()
        {
            Headers = new NameValueCollection();
        }
    }
}
