using System.Collections.Specialized;
using System.Net;

namespace Ably
{
    internal enum ResponseType
    {
        Json,
        Binary
    }

    internal class AblyResponse
    {
        internal NameValueCollection Headers { get; set; } 
        internal ResponseType Type { get; set; }
        internal HttpStatusCode StatusCode { get; set; }
        internal string TextResponse { get; set; }
        internal string ContentType { get; set; }

        internal byte[] Body { get; set; }

        internal string Encoding { get; set; }

        internal AblyResponse()
        {
            Headers = new NameValueCollection();
        }

        internal AblyResponse(string encoding, string contentType, byte[] body)
        {
            ContentType = contentType;
            Type = contentType.ToLower() == "application/json" ? ResponseType.Json : ResponseType.Binary;
            Encoding = encoding.IsNotEmpty() ? encoding : "utf-8";
            if (Type == ResponseType.Json)
            {
                TextResponse = System.Text.Encoding.GetEncoding(Encoding).GetString(body);
            }
            Body = body;
        }
    }
}
