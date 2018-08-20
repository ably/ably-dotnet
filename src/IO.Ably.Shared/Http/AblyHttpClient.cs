using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IO.Ably;

namespace IO.Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        internal Func<DateTimeOffset> Now { get; set; }

        internal ILogger Logger { get; set; }

        internal AblyHttpOptions Options { get; }

        internal string CustomHost { get; set; }

        internal HttpClient Client { get; set; }

        internal AblyHttpClient(AblyHttpOptions options, HttpMessageHandler messageHandler = null)
        {
            Now = options.NowFunc;
            Logger = options.Logger ?? IO.Ably.DefaultLogger.LoggerInstance;
            Options = options;
            CreateInternalHttpClient(options.HttpRequestTimeout, messageHandler);
        }

        internal void CreateInternalHttpClient(TimeSpan timeout, HttpMessageHandler messageHandler)
        {
            Client = messageHandler != null ? new HttpClient(messageHandler) : new HttpClient();
            Client.DefaultRequestHeaders.Add("X-Ably-Version", Defaults.ProtocolVersion);
            Client.DefaultRequestHeaders.Add("X-Ably-Lib", Defaults.LibraryVersion);
            Client.Timeout = timeout;
        }

        public async Task<AblyResponse> Execute(AblyRequest request)
        {
            var fallbackHosts = Defaults.FallbackHosts.ToList();
            if (CustomHost.IsNotEmpty())
            {
                // The custom host is a fallback host currently in use by the Realtime client.
                // We need to remove it from the fallback hosts
                fallbackHosts.Remove(CustomHost);
            }

            var random = new Random();

            int currentTry = 0;
            var startTime = Now();
            var numberOfRetries = Options.HttpMaxRetryCount;
            var host = CustomHost.IsNotEmpty() ? CustomHost : Options.Host;

            while (currentTry < numberOfRetries)
            {
                DateTimeOffset requestTime = Now();
                if ((requestTime - startTime).TotalSeconds >= Options.HttpMaxRetryDuration.TotalSeconds)
                {
                    Logger.Error("Cumulative retry timeout of {0}s was exceeded", Config.CommulativeFailedRequestTimeOutInSeconds);
                    throw new AblyException(
                        new ErrorInfo($"Commulative retry timeout of {Config.CommulativeFailedRequestTimeOutInSeconds}s was exceeded.", 50000, null));
                }

                Logger.Debug("Executing request: " + request.Url + (currentTry > 0 ? $"try {currentTry}" : string.Empty));
                try
                {
                    var message = GetRequestMessage(request, host);
                    await LogMessage(message);
                    var response = await Client.SendAsync(message, HttpCompletionOption.ResponseContentRead);
                    var ablyResponse = await GetAblyResponse(response);
                    LogResponse(ablyResponse, request.Url);

                    if (response.IsSuccessStatusCode)
                    {
                        return ablyResponse;
                    }

                    if (IsRetryableResponse(response) && Options.IsDefaultHost)
                    {
                        Logger.Warning("Failed response. Retrying. Returned response with status code: " +
                                       response.StatusCode);

                        if (TryGetNextRandomHost(fallbackHosts, random, out host))
                        {
                            Logger.Debug("Retrying using host {0}", host);
                            currentTry++;
                            continue;
                        }
                    }

                    if (request.NoExceptionOnHttpError)
                    {
                        return ablyResponse;
                    }

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new AblyException(ErrorInfo.Parse(ablyResponse), ex);
                    }

                    throw AblyException.FromResponse(ablyResponse);
                }
                catch (HttpRequestException ex) when (IsRetryableError(ex) && Options.IsDefaultHost)
                {
                    Logger.Warning("Error making a connection to Ably servers. Retrying", ex);
                    if (TryGetNextRandomHost(fallbackHosts, random, out host))
                    {
                        Logger.Debug("Retrying using host {0}", host);
                        currentTry++;
                        continue;
                    }

                    throw;
                }
                catch (TaskCanceledException ex) when (IsRetryableError(ex) && Options.IsDefaultHost)
                {
                    Logger.Warning("Error making a connection to Ably servers. Retrying", ex);
                    if (TryGetNextRandomHost(fallbackHosts, random, out host))
                    {
                        Logger.Debug("Retrying using host {0}", host);
                        currentTry++;
                        continue;
                    }

                    throw;
                }
                catch (HttpRequestException ex)
                {
                    StringBuilder reason = new StringBuilder(ex.Message);
                    var innerEx = ex.InnerException;
                    while (innerEx != null)
                    {
                        reason.Append(" " + innerEx.Message);
                        innerEx = innerEx.InnerException;
                    }

                    throw new AblyException(new ErrorInfo(reason.ToString(), 50000), ex);
                }
                catch (TaskCanceledException ex)
                {
                    // if the cancellation was not requested then this is timeout.
                    if (ex.CancellationToken.IsCancellationRequested == false)
                    {
                        throw new AblyException(new ErrorInfo("Error executing request. Request timed out.", 50000), ex);
                    }
                    else
                    {
                        throw new AblyException(new ErrorInfo("Error executing request", 50000), ex);
                    }
                }
            }

            throw new AblyException(new ErrorInfo("Error exectuting request", 50000));
        }

        private void LogResponse(AblyResponse ablyResponse, string url)
        {
            if (Logger.IsDebug == false)
            {
                return;
            }

            StringBuilder logMessage = new StringBuilder($"Response from: {url}");
            logMessage.AppendLine($"Status code: {(int)ablyResponse.StatusCode} {ablyResponse.StatusCode}");

            logMessage.AppendLine("---- Response Headers ----");
            foreach (var header in ablyResponse.Headers)
            {
                logMessage.AppendLine($"{header.Key}: {header.Value.JoinStrings()}");
            }

            logMessage.AppendLine($"Content Type: {ablyResponse.ContentType}");
            logMessage.AppendLine($"Encoding: {ablyResponse.Encoding}");
            logMessage.AppendLine($"Type: {ablyResponse.Type}");

            logMessage.AppendLine("---- Response Body ----");
            if (ablyResponse.Type != ResponseType.Binary)
            {
                logMessage.AppendLine(ablyResponse.TextResponse);
            }
            else if (ablyResponse.Body != null)
            {
                logMessage.AppendLine(ablyResponse.Body.GetText());
            }

            Logger.Debug(logMessage.ToString());
        }

        private async Task LogMessage(HttpRequestMessage message)
        {
            if (Logger.IsDebug == false)
            {
                return;
            }

            StringBuilder logMessage = new StringBuilder();
            if (message.Headers.Any())
            {
                logMessage.AppendLine("---- Headers ----");
                foreach (var header in message.Headers)
                {
                    logMessage.AppendLine($"{header.Key}:{header.Value.JoinStrings()}");
                }
            }

            if (message.Content != null)
            {
                var body = await message.Content.ReadAsStringAsync();
                logMessage.AppendLine("---- Body ----");
                logMessage.AppendLine(body);
            }

            Logger.Debug(logMessage.ToString());
        }

        private bool TryGetNextRandomHost(List<string> hosts, Random random, out string host)
        {
            if (hosts.Count == 0)
            {
                host = string.Empty;
                return false;
            }

            host = hosts[random.Next() % hosts.Count];
            hosts.Remove(host);
            return true;
        }

        internal bool IsRetryableResponse(HttpResponseMessage response)
        {
            return ErrorInfo.IsRetryableStatusCode(response.StatusCode);
        }

        internal bool IsRetryableError(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                return true;
            }

            var httpEx = ex as HttpRequestException;
            if (httpEx?.InnerException is WebException)
            {
                var webEx = httpEx.InnerException as WebException;
                return webEx.Status == WebExceptionStatus.NameResolutionFailure ||
                    webEx.Status == WebExceptionStatus.Timeout ||
                    webEx.Status == WebExceptionStatus.ConnectFailure ||
                    webEx.Status == WebExceptionStatus.ReceiveFailure ||
                    webEx.Status == WebExceptionStatus.ConnectionClosed ||
                    webEx.Status == WebExceptionStatus.SendFailure ||
                    webEx.Status == WebExceptionStatus.ServerProtocolViolation;
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

#if MSGPACK
            if(request.Protocol == Protocol.MsgPack)
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(GetHeaderValue(request.Protocol)));
#endif

            // Always accept JSON
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

            var ablyResponse = new AblyResponse(contentTypeHeader?.CharSet, contentTypeHeader?.MediaType, content)
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers
            };
            return ablyResponse;
        }

        public Uri GetRequestUrl(AblyRequest request, string host = null)
        {
            if (host == null)
            {
                host = Options.Host;
            }

            string protocol = Options.IsSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
            {
                return new Uri(request.Url);
            }

            return new Uri($"{protocol}{host}{(Options.Port.HasValue ? ":" + Options.Port.Value : string.Empty)}{request.Url}{GetQuery(request)}");
        }

        private string GetQuery(AblyRequest request)
        {
            var query = request.QueryParameters.ToQueryString();
            if (query.IsNotEmpty())
            {
                return "?" + query;
            }

            return string.Empty;
        }
    }
}
