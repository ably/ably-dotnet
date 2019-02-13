using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using System.Threading.Tasks;
using IO.Ably;
using Newtonsoft.Json.Linq;

namespace IO.Ably
{
    /// <summary>Client for the ably rest API</summary>
    public sealed class AblyRest : IRestClient
    {
        internal AblyHttpClient HttpClient { get; private set; }

        internal MessageHandler MessageHandler { get; private set; }

        internal string CustomHost
        {
            get { return HttpClient.CustomHost; }
            set { HttpClient.CustomHost = value; }
        }

        internal AblyAuth AblyAuth { get; private set; }

        public RestChannels Channels { get; private set; }

        /// <summary>
        /// Authentication methods
        /// </summary>
        public IAblyAuth Auth => AblyAuth;

        internal Protocol Protocol => Options.UseBinaryProtocol == false ? Protocol.Json : Defaults.Protocol;

        internal ClientOptions Options { get; }

        internal ILogger Logger { get; set; }

        /// <summary>Initializes the RestClient using the api key provided</summary>
        /// <param name="apiKey">Full api key</param>
        public AblyRest(string apiKey)
            : this(new ClientOptions(apiKey))
        {
        }

        /// <summary>
        /// Convenience method for initializing the RestClient by passing a Action{ClientOptions}
        /// <example>
        /// var rest = new AblyRest(opt => {
        ///  opt.Key = "fake.key:value";
        ///  opt.ClientId = "123";
        /// });
        /// </example>
        /// </summary>
        /// <param name="init">Action delegate which receives a empty options object.</param>
        public AblyRest(Action<ClientOptions> init)
        {
            Options = new ClientOptions();
            init(Options);
            InitializeAbly();
        }

        /// <summary>
        /// Initialize the library with a custom set of options
        /// </summary>
        /// <param name="clientOptions"></param>
        public AblyRest(ClientOptions clientOptions)
        {
            Options = clientOptions;
            InitializeAbly();
        }

        /// <summary>Initializes the rest client and validates the passed in options</summary>
        private void InitializeAbly()
        {
            if (Options == null)
            {
                Logger.Error("No options provider to Ably rest");
                throw new AblyException("Invalid options");
            }

            Logger = Options.Logger ?? IO.Ably.DefaultLogger.LoggerInstance;

            if (Options.LogLevel.HasValue)
            {
                Logger.LogLevel = Options.LogLevel.Value;
            }

            if (Options.LogHander != null)
            {
                Logger.LoggerSink = Options.LogHander;
            }

            Logger.Debug("Protocol set to: " + Protocol);
            MessageHandler = new MessageHandler(Protocol);

            HttpClient = new AblyHttpClient(new AblyHttpOptions(Options));
            ExecuteHttpRequest = HttpClient.Execute;
            AblyAuth = new AblyAuth(Options, this);
            Channels = new RestChannels(this);
        }

        internal Func<AblyRequest, Task<AblyResponse>> ExecuteHttpRequest;

        internal async Task<AblyResponse> ExecuteRequest(AblyRequest request)
        {
            Logger.Debug("Sending {0} request to {1}", request.Method, request.Url);

            if (request.SkipAuthentication == false)
            {
                await AblyAuth.AddAuthHeader(request);
            }

            try
            {
                MessageHandler.SetRequestBody(request);
                return await ExecuteHttpRequest(request);
            }
            catch (AblyException ex)
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Error Executing request. Message: " + ex.Message);
                }

                if (ex.ErrorInfo.IsUnAuthorizedError
                    && ex.ErrorInfo.IsTokenError
                    && AblyAuth.TokenRenewable)
                {
                    if (Logger.IsDebug)
                    {
                        Logger.Debug("Handling UnAuthorized Error, attmepting to Re-authorize and repeat request.");
                    }

                    try
                    {
                        await AblyAuth.AuthorizeAsync(null, new AuthOptions());
                        await AblyAuth.AddAuthHeader(request);
                        return await ExecuteHttpRequest(request);
                    }
                    catch (AblyException ex2)
                    {
                        throw new AblyException(ex2.ErrorInfo, ex);
                    }
                }

                throw;
            }
            catch (Exception ex)
            {
                if (Logger.IsDebug)
                {
                    Logger.Debug("Error Executing request. Message: " + ex.Message);
                }

                throw new AblyException(ex);
            }
        }

        internal async Task<T> ExecuteRequest<T>(AblyRequest request) where T : class
        {
            var response = await ExecuteRequest(request);
            if (Logger.IsDebug)
            {
                Logger.Debug("Response received. Status: " + response.StatusCode);
                Logger.Debug("Content type: " + response.ContentType);
                Logger.Debug("Encoding: " + response.Encoding);
                if (response.Body != null)
                {
                    Logger.Debug("Raw response (base64):" + response.Body.ToBase64());
                }
            }

            return MessageHandler.ParseResponse<T>(request, response);
        }

        internal async Task<PaginatedResult<T>> ExecutePaginatedRequest<T>(AblyRequest request, Func<PaginatedRequestParams, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            var response = await ExecuteRequest(request);
            if (Logger.IsDebug)
            {
                Logger.Debug("Response received. Status: " + response.StatusCode);
                Logger.Debug("Content type: " + response.ContentType);
                Logger.Debug("Encoding: " + response.Encoding);
                if (response.Body != null)
                {
                    Logger.Debug("Raw response (base64):" + response.Body.ToBase64());
                }
            }

            return MessageHandler.ParsePaginatedResponse<T>(request, response, executeDataQueryRequest);
        }

        internal async Task<HttpPaginatedResponse> ExecuteHttpPaginatedRequest(AblyRequest request, PaginatedRequestParams requestParams, Func<PaginatedRequestParams, Task<HttpPaginatedResponse>> executeDataQueryRequest)
        {
            var response = await ExecuteRequest(request);

            if (Logger.IsDebug)
            {
                Logger.Debug("Response received. Status: " + response.StatusCode);
                Logger.Debug("Content type: " + response.ContentType);
                Logger.Debug("Encoding: " + response.Encoding);
                if (response.Body != null)
                {
                    Logger.Debug("Raw response (base64):" + response.Body.ToBase64());
                }
            }

            return MessageHandler.ParseHttpPaginatedResponse(request, response, requestParams, executeDataQueryRequest);
        }

        internal async Task<HttpPaginatedResponse> HttpPaginatedRequestInternal(PaginatedRequestParams requestParams)
        {
            var request = CreateRequest(requestParams.Path, requestParams.HttpMethod);
            request.NoExceptionOnHttpError = true;
            request.AddQueryParameters(requestParams.ExtraParameters);
            request.AddHeaders(requestParams.Headers);
            if (requestParams.Body != null)
            {
                request.PostData = requestParams.Body;
            }

            return await ExecuteHttpPaginatedRequest(request, requestParams, HttpPaginatedRequestInternal);
        }

        public async Task<HttpPaginatedResponse> Request(string method, string path, Dictionary<string, string> requestParams = null, JToken body = null, Dictionary<string, string> headers = null)
        {
            var httpMethod = new HttpMethod(method);
            return await Request(httpMethod, path, requestParams, body, headers);
        }

        public async Task<HttpPaginatedResponse> Request(HttpMethod method, string path, Dictionary<string, string> requestParams = null, JToken body = null, Dictionary<string, string> headers = null)
        {
            var p = new PaginatedRequestParams();
            p.Headers = headers;
            p.ExtraParameters = requestParams;
            p.Body = body;
            p.HttpMethod = method;
            p.Path = path;

            return await HttpPaginatedRequestInternal(p);
        }

        /// <summary>/// Retrieves the ably service time/// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> TimeAsync()
        {
            AblyRequest request = CreateGetRequest("/time");
            request.SkipAuthentication = true;
            List<long> response = await ExecuteRequest<List<long>>(request);
            return response.First().FromUnixTimeInMilliseconds();
        }

        /// <summary>
        /// Retrieves the stats for the application. Passed default <see cref="StatsRequestParams"/> for the request
        /// </summary>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return StatsAsync(new StatsRequestParams());
        }

        /// <summary>
        /// Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsRequestParams"/> for more information
        /// </summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> StatsAsync(StatsRequestParams query)
        {
            return StatsAsync(query as PaginatedRequestParams);
        }

        /// <summary>
        /// Retrieves the stats for the application based on a custom query. It should be used with <see cref="PaginatedRequestParams"/>.
        /// It is mainly because of the way a PaginatedResource defines its queries. For retrieving Stats with special parameters use <see cref="StatsAsync(StatsRequestParams)"/>
        /// </summary>
        /// <example>
        /// var client = new AblyRest("validkey");
        /// var stats = client..StatsAsync();
        /// var nextPage = cliest..StatsAsync(stats.NextQuery);
        /// </example>
        /// <param name="query"><see cref="PaginatedRequestParams"/> and <see cref="StatsRequestParams"/></param>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> StatsAsync(PaginatedRequestParams query)
        {
            query.Validate();

            var request = CreateGetRequest("/stats");

            request.AddQueryParameters(query.GetParameters());

            return ExecutePaginatedRequest(request, StatsAsync);
        }

        internal AblyRequest CreateRequest(string path, HttpMethod method, ChannelOptions options = null)
        {
            return new AblyRequest(path, method, Protocol) { ChannelOptions = options };
        }

        internal AblyRequest CreateGetRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Get, Protocol) { ChannelOptions = options };
        }

        internal AblyRequest CreatePostRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Post, Protocol) { ChannelOptions = options };
        }

        public async Task<bool> CanConnectToAbly()
        {
            if (Options.SkipInternetCheck)
            {
                return true;
            }

            try
            {
                var request = new AblyRequest(Defaults.InternetCheckUrl, HttpMethod.Get);
                var response = await ExecuteHttpRequest(request);
                return response.TextResponse == Defaults.InternetCheckOkMessage;
            }
            catch (Exception ex)
            {
                Logger.Error("Error accessing ably internet check url. Internet is down!", ex);
                return false;
            }
        }

        public PaginatedResult<Stats> Stats()
        {
            return AsyncHelper.RunSync(StatsAsync);
        }

        public PaginatedResult<Stats> Stats(StatsRequestParams query)
        {
            return AsyncHelper.RunSync(() => StatsAsync(query));
        }

        public DateTimeOffset Time()
        {
            return AsyncHelper.RunSync(TimeAsync);
        }
    }
}
