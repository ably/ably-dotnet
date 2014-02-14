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
        public string TextResponse { get; set; }

        private byte[] _body;
        public byte[] Body
        {
            get { return _body; }
            set
            {
                if (Type == ResponseType.Json)
                {
                    TextResponse = System.Text.Encoding.GetEncoding(Encoding).GetString(value);
                }
                _body = value;
            }
        }

        public string Encoding { get; set; }

        public AblyResponse()
        {
            Headers = new NameValueCollection();
        }
    }
}
