using System;
using System.Collections.Generic;
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
        public ResponseType Type { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string JsonResult { get; set; }
    }
}
