using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace Ably
{
    public enum ResponseType
    {
        Json,
        Thrift
    }

    internal class AblyResponse
    {
        internal NameValueCollection Headers { get; set; } 
        internal ResponseType Type { get; set; }
        internal HttpStatusCode StatusCode { get; set; }
        internal string TextResponse { get; set; }

        internal byte[] Body { get; set; }

        internal string Encoding { get; set; }

        internal AblyResponse()
        {
            Headers = new NameValueCollection();
        }

        internal AblyResponse(string encoding, string contentType, byte[] body)
        {
            Type = contentType.ToLower() == "application/json" ? ResponseType.Json : ResponseType.Thrift;
            Encoding = encoding.IsNotEmpty() ? encoding : "utf-8";
            if (Type == ResponseType.Json)
            {
                TextResponse = System.Text.Encoding.GetEncoding(Encoding).GetString(body);
            }
            Body = body;
        }
    }
}
