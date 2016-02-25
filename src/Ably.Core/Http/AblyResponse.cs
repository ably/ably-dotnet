using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace IO.Ably
{
    internal enum ResponseType
    {
        Json,
        Binary
    }

    internal class AblyResponse
    {
        internal WebHeaderCollection Headers { get; set; }
        internal ResponseType Type { get; set; }
        internal HttpStatusCode StatusCode { get; set; }
        internal string TextResponse { get; set; }
        internal string ContentType { get; set; }

        internal byte[] Body { get; set; }

        internal string Encoding { get; set; }

        internal AblyResponse()
        {
            Headers = new WebHeaderCollection();
        }

        static void fixContentType( ref string encoding, ref string contentType )
        {
            if( contentType.IndexOf(';') >= 0 )
            {
                string[] parts = contentType.Split( new char[ 1 ] { ';' }, StringSplitOptions.RemoveEmptyEntries );
                if ( parts.Length <= 0 )
                    throw new Exception( "Malformed contentType " + contentType );
                contentType = parts[ 0 ];
                string charsetPart = parts.Skip( 1 ).FirstOrDefault( p => p.ToLowerInvariant().Contains( "charset" ) );
                if( charsetPart.notEmpty() )
                {
                    encoding = charsetPart.Split( '=' )[ 1 ];
                }
            }
            char[] trimChars = "\"\' \t".ToCharArray();
            contentType = contentType.Trim( trimChars );
            if ( encoding.isEmpty() )
                encoding = "utf-8";
            else
                encoding = encoding.Trim( trimChars );
        }

        internal AblyResponse(string encoding, string contentType, byte[] body)
        {
            fixContentType( ref encoding, ref contentType );

            ContentType = contentType;
            Type = contentType.ToLower() == "application/json" ? ResponseType.Json : ResponseType.Binary;
            Encoding = encoding.IsNotEmpty() ? encoding : "utf-8";
            if (Type == ResponseType.Json)
            {
                TextResponse = System.Text.Encoding.GetEncoding( Encoding ).GetString( body, 0, body.Length );
            }
            Body = body;
        }
    }
}
