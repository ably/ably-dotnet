using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Threading.Tasks;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        internal AblyHttpOptions Options { get; }

        internal HttpClient Client { get; set; }

        internal AblyHttpClient(AblyHttpOptions options, HttpMessageHandler messageHandler =null)
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
            var fallbackHosts = Defaults.FallbackHosts.ToList();
            var random = new Random();

            int currentTry = 0;
            var startTime = Config.Now();
            var numberOfRetries = Options.HttpMaxRetryCount;
            var host = Options.Host;

            while (currentTry < numberOfRetries)
            {
                var requestTime = Config.Now();
                if ((requestTime - startTime).TotalSeconds >= Options.HttpMaxRetryDuration.TotalSeconds)
                {
                    Logger.Error("Cumulative retry timeout of {0}s was exceeded", Config.CommulativeFailedRequestTimeOutInSeconds);
                    throw new AblyException(
                        new ErrorInfo(string.Format("Commulative retry timeout of {0}s was exceeded.",
                            Config.CommulativeFailedRequestTimeOutInSeconds), 500, null));
                }

                Logger.Debug("Executing request: " + request.Url + (currentTry > 0 ? $"try {currentTry}" : ""));
                try
                {
                    var message = GetRequestMessage(request, host);
                    var response = await Client.SendAsync(message, HttpCompletionOption.ResponseContentRead);

                    var ablyResponse = await GetAblyResponse(response);
                    if (response.IsSuccessStatusCode)
                    {
                        return ablyResponse;
                    }

                    if (IsRetryableResponse(response) && Options.IsDefaultHost)
                    {
                        Logger.Info("Failed response. Retrying. Returned response with status code: " +
                                    response.StatusCode);

                        if (TryGetNextRandomHost(fallbackHosts, random, out host))
                        {
                            Logger.Info("Retrying using host {0}", host);
                            currentTry++;
                            continue;
                        }
                    }
                    throw AblyException.FromResponse(ablyResponse);
                }
                catch (HttpRequestException ex) when(IsRetryableError(ex) && Options.IsDefaultHost)
                {
                    Logger.Warning("Error making a connection to Ably servers. Retrying", ex);

                    if (TryGetNextRandomHost(fallbackHosts, random, out host))
                    {
                        Logger.Info("Retrying using host {0}", host);
                        currentTry++;
                        continue;
                    }
                    throw;
                    //_host = hosts[currentTry - 1];
                    
                }
                catch (TaskCanceledException ex) when (IsRetryableError(ex) && Options.IsDefaultHost)
                {
                    //TODO: Check about the conditions we should retry. 
                    //First retry the same host and then start the others
                    
                    Logger.Warning("Error making a connection to Ably servers. Retrying", ex);
                    if (TryGetNextRandomHost(fallbackHosts, random, out host))
                    {
                        Logger.Info("Retrying using host {0}", host);
                        currentTry++;
                        continue;
                    }
                    throw;
                }
                catch(HttpRequestException ex) { throw new AblyException(new ErrorInfo("Error executing request", 500), ex);}
                catch(TaskCanceledException ex) { throw new AblyException(new ErrorInfo("Error executing request", 500), ex);}
            }

            throw new AblyException(new ErrorInfo("Error exectuting request", 500));
        }

        private bool TryGetNextRandomHost(List<string> hosts, Random random, out string host)
        {
            if (hosts.Count == 0)
            {
                host = "";
                return false;
            }

            host = hosts[random.Next(1000) % hosts.Count];
            hosts.Remove(host);
            return true;
        }

        internal bool IsRetryableResponse(HttpResponseMessage response)
        {
            if (response.StatusCode >= (HttpStatusCode) 500 && response.StatusCode <= (HttpStatusCode) 504)
                return true;
            return false;
        }

        internal bool IsRetryableError(Exception ex)
        {
            if (ex is TaskCanceledException)
                return true;
            var httpEx = ex as HttpRequestException;
            if (httpEx?.InnerException is WebException)
            {
                var webEx = httpEx.InnerException as WebException;
                return webEx.Status == WebExceptionStatus.NameResolutionFailure || 
                    webEx.Status == WebExceptionStatus.Timeout ||
                    webEx.Status == WebExceptionStatus.ConnectFailure;
            }
            return false;
        }

        private HttpRequestMessage GetRequestMessage(AblyRequest request, string host)
        {
            var message = new HttpRequestMessage(request.Method, GetRequestUrl(request, host));

            foreach (var header in request.Headers)
            {
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if(request.Protocol == Protocol.MsgPack)
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(GetHeaderValue(request.Protocol)));

            //Always accept JSON
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(GetHeaderValue(Protocol.Json))); 
            if (message.Method == HttpMethod.Post)
            {
                if (request.PostParameters.Any() && request.RequestBody.Length == 0)
                {
                    message.Content = new FormUrlEncodedContent(request.PostParameters);
                }
                else
                {
                    var content = new ByteArrayContent(request.RequestBody);
                    content.Headers.ContentType = new MediaTypeHeaderValue(GetHeaderValue(request.Protocol));
                    message.Content = content;
                }
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

        public Uri GetRequestUrl(AblyRequest request, string host = null)
        {
            if (host == null)
                host = Options.Host;

            string protocol = Options.IsSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(string.Format("{0}{1}{2}{3}{4}",
                               protocol,
                               host,
                               Options.Port.HasValue ? ":" + Options.Port.Value : "",
                               request.Url,
                               GetQuery(request)));
        }

        private string GetQuery(AblyRequest request)
        {
            var query = request.QueryParameters.ToQueryString();
            if (query.IsNotEmpty())
                return "?" + query;
            return string.Empty;
        }
    }


}