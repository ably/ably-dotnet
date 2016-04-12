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
        private bool IsDefaultHost => Options.Host == Defaults.RestHost;

        internal AblyHttpOptions Options { get; }

        internal HttpClient Client { get; set; }

        public AblyHttpClient(AblyHttpOptions options) : 
            this(options, null)
        {
            
        }

        internal AblyHttpClient(AblyHttpOptions options, HttpMessageHandler messageHandler)
        {
            Options = options;
            CreateInternalHttpClient(options.HttpRequestTimeout, messageHandler);
        }

        internal void CreateInternalHttpClient(TimeSpan timeout, HttpMessageHandler messageHandler)
        {
            Client = messageHandler != null ? new HttpClient(messageHandler) : new HttpClient();
            Client.DefaultRequestHeaders.Add("X-Ably-Version", Config.AblyVersion);
            Client.Timeout = timeout;
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

                var response = await Client.SendAsync(message, HttpCompletionOption.ResponseContentRead);

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
            string protocol = Options.IsSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(String.Format("{0}{1}{2}{3}{4}",
                               protocol,
                               GetHost(),
                               Options.Port.HasValue ? ":" + Options.Port.Value : "",
                               request.Url,
                               GetQuery(request)));
        }

        private string GetHost()
        {
            if (IsDefaultHost && (Options.Environment.HasValue && Options.Environment != AblyEnvironment.Live))
                return Options.Environment.ToString().ToLower() + "-" + Options.Host;
            return Options.Host;
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