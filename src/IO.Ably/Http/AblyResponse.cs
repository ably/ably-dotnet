using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;

namespace IO.Ably
{
    internal class EmptyHttpHeaders : HttpHeaders
    {
        
    }


    internal enum ResponseType
    {
        Json,
        Binary
    }

    internal class AblyResponse
    {
        internal HttpHeaders Headers { get; set; }
        internal ResponseType Type { get; set; }
        internal HttpStatusCode StatusCode { get; set; }
        internal string TextResponse { get; set; }
        internal string ContentType { get; set; }

        internal byte[] Body { get; set; }

        internal string Encoding { get; set; }

        internal AblyResponse()
        {
            Headers = new EmptyHttpHeaders();
        }

        

        internal AblyResponse(string encoding, string contentType, byte[] body)
        {
            

            ContentType = contentType;
            Type = contentType.ToLower() == "application/json" ? ResponseType.Json : ResponseType.Binary;
            Encoding = StringExtensions.IsNotEmpty(encoding) ? encoding : "utf-8";
            if (Type == ResponseType.Json)
            {
                TextResponse = System.Text.Encoding.GetEncoding( Encoding ).GetString( body, 0, body.Length );
            }
            Body = body;
        }
    }
}
