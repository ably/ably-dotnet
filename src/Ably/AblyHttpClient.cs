using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Ably
{
    public class AblyHttpClient : IAblyHttpClient
    {
        private readonly string _Host;
        private readonly int? _Port;
        private readonly bool _IsSecure;

        
        public AblyHttpClient(string host) : this(host, null, true) { }

        public AblyHttpClient(string host, int? port = null, bool isSecure = true)
        {
            _IsSecure = isSecure;
            _Port = port;
            _Host = host;
        }

        public AblyResponse Execute(AblyRequest request)
        {
            var webRequest = HttpWebRequest.Create(GetRequestUrl(request)) as HttpWebRequest;
            foreach (var header in request.Headers)
            {
                if (header.Key == "Accept")
                    webRequest.Accept = header.Value;
                else if (header.Key == "Content-Type")
                    webRequest.ContentType = header.Value;
                else
                    webRequest.Headers.Add(header.Key, header.Value);
            }
            webRequest.UserAgent = "Ably.net library";
            webRequest.Method = request.Method.Method;
            

            string requestBody = "";
            if (request.PostData != null)
            {
                requestBody = request.PostData is string ? (string)request.PostData : JsonConvert.SerializeObject(request.PostData);
            }
            else if (request.PostParameters.Count > 0)
            {
                requestBody = string.Join("&", request.PostParameters.Select(x => x.Key + "=" + x.Value));
            }

            if (requestBody.IsNotEmpty())
            {

                var body = Encoding.UTF8.GetBytes(requestBody);
                webRequest.ContentLength = body.Length;
                //webRequest.SendChunked = true;
                //webRequest.TransferEncoding = "utf-8";
                using (Stream stream = webRequest.GetRequestStream())
                {
                    stream.Write(body, 0, body.Length);
                }
            }
            else
                webRequest.ContentLength = 0;

            using (var response = webRequest.GetResponse() as HttpWebResponse)
            {
                var ablyResponse = new AblyResponse();
                ablyResponse.Type = response.ContentType == "application/json" ? ResponseType.Json : ResponseType.Thrift;
                ablyResponse.StatusCode = response.StatusCode;
                string encoding = response.ContentEncoding.IsNotEmpty() ? response.ContentEncoding : "utf-8";
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
                {
                    ablyResponse.JsonResult = reader.ReadToEnd();
                }
                return ablyResponse;
            }
        }

        private Uri GetRequestUrl(AblyRequest request)
        {
            string protocol = _IsSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(String.Format("{0}{1}{2}{3}{4}", 
                               protocol, 
                               _Host, 
                               _Port.HasValue ? ":" + _Port.Value : "",
                               request.Url, 
                               GetQuery(request)));
        }

        private object GetQuery(AblyRequest request)
        {
            string query = string.Join("&", request.QueryParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            if(query.IsNotEmpty())
                return "?" + query;
            return string.Empty;
        }
    }
}
