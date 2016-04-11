using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        //TODO: MG make sure this handles the fallback hosts as well
        private bool IsDefaultHost => Host == Defaults.RestHost;

        public bool IsSecure { get; }
        public string Host { get; }
        public int? Port { get; }
        public AblyEnvironment? Environment { get; }

        private HttpClient _client;

        public AblyHttpClient(string host = null, int? port = null, bool isSecure = true, AblyEnvironment? environment = AblyEnvironment.Live) : 
            this(host, port, isSecure, environment, null)
        {
            
        }

        internal AblyHttpClient(string host, int? port, bool isSecure, AblyEnvironment? environment, HttpMessageHandler messageHandler)
        {
            IsSecure = isSecure;
            Environment = environment;
            Port = port;
            Host = host ?? Defaults.RestHost;

            _client = messageHandler != null ? new HttpClient(messageHandler) : new HttpClient();
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

            foreach (var header in request.Headers)
            {
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

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

        public Uri GetRequestUrl(AblyRequest request)
        {
            string protocol = IsSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(String.Format("{0}{1}{2}{3}{4}",
                               protocol,
                               GetHost(),
                               Port.HasValue ? ":" + Port.Value : "",
                               request.Url,
                               GetQuery(request)));
        }

        private string GetHost()
        {
            if (IsDefaultHost && (Environment.HasValue && Environment != AblyEnvironment.Live))
                return Environment.ToString().ToLower() + "-" + Host;
            return Host;
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