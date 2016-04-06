using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ModernHttpClient;

namespace IO.Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        internal static readonly MimeTypes MimeTypes = new MimeTypes();

        private string _host;
        private readonly int? _port;
        private readonly bool _isSecure;
        private readonly AblyEnvironment? _environment;

        //TODO: MG make sure this handles the fallback hosts as well
        private bool IsDefaultHost => _host == Config.DefaultHost;
        private HttpClient _client;

        public AblyHttpClient(string host) : this(host, null, true) { }

        public AblyHttpClient(string host, int? port = null, bool isSecure = true, AblyEnvironment? environment = AblyEnvironment.Live)
        {
            _isSecure = isSecure;
            _environment = environment;
            _port = port;
            _host = host;
            _client = new HttpClient(new NativeMessageHandler());
            _client.DefaultRequestHeaders.Add("X-Ably-Version", Config.AblyVersion);
            _client.Timeout = TimeSpan.FromSeconds(Config.ConnectTimeout);
        }

        public async Task<AblyResponse> Execute(AblyRequest request)
        {
            //TODO: MG Add retrying back
            //var hosts = Config.FallbackHosts;

            //int currentTry = 0;
            //var startTime = Config.Now();
            //while (currentTry <= hosts.Length)
            //{
            //    var requestTime = Config.Now();
            //    if ((requestTime - startTime).TotalSeconds > Config.CommulativeFailedRequestTimeOutInSeconds)
            //    {
            //        Logger.Error("Cumulative retry timeout of {0}s was exceeded", Config.CommulativeFailedRequestTimeOutInSeconds);
            //        throw new AblyException(
            //            new ErrorInfo(string.Format("Commulative retry timeout of {0}s was exceeded.",
            //                Config.CommulativeFailedRequestTimeOutInSeconds), 500, null));
            //    }

            try
            {
                var message = GetRequestMessage(request);

                var response = await _client.SendAsync(message, HttpCompletionOption.ResponseContentRead);

                var ablyResponse = await GetAblyResponse(response);
                if (response.IsSuccessStatusCode)
                {
                    return ablyResponse;
                }
                
                throw AblyException.FromResponse(ablyResponse);

            }
            catch (HttpRequestException ex)
            {
                //TODO: Check about the conditions we should retry. 
                //First retry the same host and then start the others
                //if (IsRetryableAndNotExpiredToken(response) && IsDefaultHost)
                //{
                //    Logger.Error("Error making a connection to Ably servers. Retrying", exception);
                //    _host = hosts[currentTry - 1];
                //    currentTry++;
                //    continue;
                //}
                throw new AblyException(new ErrorInfo("Error exectuting request", 500), ex);
            }
            //}
        }

        private HttpRequestMessage GetRequestMessage(AblyRequest request)
        {
            var message = new HttpRequestMessage(request.Method, GetRequestUrl(request));
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(GetHeaderValue(request.Protocol)));
            if (message.Method == HttpMethod.Post)
            {
                var content = new ByteArrayContent(request.RequestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue(GetHeaderValue(request.Protocol));
                message.Content = content;
            }

            return message;
        }

        internal static string GetHeaderValue(Protocol protocol)
        {
            if (protocol == Protocol.Json)
            {
                return "application/json";
            }
            return "application/x-msgpack";
        }

        private static async Task<AblyResponse> GetAblyResponse(HttpResponseMessage response)
        {
            var contentTypeHeader = response.Content.Headers.ContentType;

            var content = await response.Content.ReadAsByteArrayAsync();

            return new AblyResponse(contentTypeHeader.CharSet, contentTypeHeader.MediaType, content)
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers
            };
        }

        private Uri GetRequestUrl(AblyRequest request)
        {
            string protocol = _isSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(String.Format("{0}{1}{2}{3}{4}",
                               protocol,
                               GetHost(),
                               _port.HasValue ? ":" + _port.Value : "",
                               request.Url,
                               GetQuery(request)));
        }

        private string GetHost()
        {
            if (IsDefaultHost && (_environment.HasValue && _environment != AblyEnvironment.Live))
                return _environment.ToString().ToLower() + "-" + _host;
            return _host;
        }

        private string GetQuery(AblyRequest request)
        {
            string query = string.Join("&", request.QueryParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            if (StringExtensions.IsNotEmpty(query))
                return "?" + query;
            return string.Empty;
        }
    }
}