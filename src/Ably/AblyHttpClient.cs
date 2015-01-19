using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        internal static readonly MimeTypes MimeTypes = new MimeTypes();

        private readonly string _host;
        private readonly int? _port;
        private readonly bool _isSecure;
        private readonly MessageHandler _messageHandler;

        public AblyHttpClient(string host) : this(host, null, true, Protocol.MsgPack) { }

        public AblyHttpClient(string host, int? port = null, bool isSecure = true, Protocol protocol = Protocol.MsgPack)
        {
            _isSecure = isSecure;
            _port = port;
            _host = host;
            _messageHandler = new MessageHandler(protocol);
        }

        public AblyResponse Execute(AblyRequest request)
        {
            var webRequest = HttpWebRequest.Create(GetRequestUrl(request)) as HttpWebRequest;
            HttpWebResponse response = null;

            PopulateDefaultHeaders(request, webRequest);
            PopulateWebRequestHeaders(webRequest, request.Headers);

            webRequest.UserAgent = "Ably.net library";
            webRequest.Method = request.Method.Method;

            try
            {
                if (webRequest.Method == "POST")
                {
                    var requestBody = _messageHandler.GetRequestBody(request);

                    webRequest.ContentLength = requestBody.Length;
                    if (requestBody.Any())
                    {
                        using (Stream stream = webRequest.GetRequestStream())
                        {
                            stream.Write(requestBody, 0, requestBody.Length);
                        }
                    }
                }

                response = webRequest.GetResponse() as HttpWebResponse;
                return GetAblyResponse(response);
            }
            catch (WebException exception)
            {
                var errorResponse = exception.Response as HttpWebResponse;
                if (errorResponse != null)
                    throw AblyException.FromResponse(GetAblyResponse(errorResponse));
                else
                    throw new AblyException(new ErrorInfo("Unexpected error. Check the inner exception for details", 500, null), exception);
            }
            finally
            {
                if (response != null)
                    response.Dispose();
            }
        }

        private void PopulateDefaultHeaders(AblyRequest request, HttpWebRequest webRequest)
        {
            if (request.Method == HttpMethod.Post)
            {
                PopulateWebRequestHeaders(webRequest, GetDefaultPostHeaders(request.Protocol));
            }
            if (request.Method == HttpMethod.Get)
            {
                PopulateWebRequestHeaders(webRequest, GetDefaultHeaders(request.Protocol));
            }
        }

        private static void PopulateWebRequestHeaders(HttpWebRequest webRequest, IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                if (header.Key == "Accept")
                    webRequest.Accept = header.Value;
                else if (header.Key == "Content-Type")
                    webRequest.ContentType = header.Value;
                else
                    webRequest.Headers.Add(header.Key, header.Value);
            }
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultHeaders(Protocol protocol)
        {
            if (protocol == Protocol.Json)
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("json"));
            }
            else
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("binary", "json"));
            }
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultPostHeaders(Protocol protocol)
        {
            if (protocol == Protocol.Json)
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("json"));
                yield return new KeyValuePair<string, string>("Content-Type", MimeTypes.GetHeaderValue("json"));
            }
            else
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("binary", "json"));
                yield return new KeyValuePair<string, string>("Content-Type", MimeTypes.GetHeaderValue("binary"));
            }
        }

        private static AblyResponse GetAblyResponse(HttpWebResponse response)
        {
            return new AblyResponse(response.ContentEncoding, response.ContentType, ReadFully(response.GetResponseStream()))
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers
            };
        }

        private static byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private Uri GetRequestUrl(AblyRequest request)
        {
            string protocol = _isSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(String.Format("{0}{1}{2}{3}{4}",
                               protocol,
                               _host,
                               _port.HasValue ? ":" + _port.Value : "",
                               request.Url,
                               GetQuery(request)));
        }

        private string GetQuery(AblyRequest request)
        {
            string query = string.Join("&", request.QueryParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            if (query.IsNotEmpty())
                return "?" + query;
            return string.Empty;
        }
    }
}
