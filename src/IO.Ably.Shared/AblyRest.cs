using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using System.Threading.Tasks;

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
        internal Protocol Protocol => Options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;

        internal ClientOptions Options { get; }

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
            Logger.Info("Sending {0} request to {1}", request.Method, request.Url);

            if (request.SkipAuthentication == false)
                await AblyAuth.AddAuthHeader(request);

            MessageHandler.SetRequestBody(request);

            try
            {
                return await ExecuteHttpRequest(request);
            }
            catch (AblyException ex)
            {
                if (ex.ErrorInfo.IsUnAuthorizedError
                    && ex.ErrorInfo.IsTokenError && AblyAuth.TokenRenewable)
                {
                    await AblyAuth.AuthoriseAsync(null, new AuthOptions() { Force = true });
                    await AblyAuth.AddAuthHeader(request);
                    return await ExecuteHttpRequest(request);
                }
                throw;
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
                    Logger.Debug("Raw response (base64):" + response.Body.ToBase64());
            }

            return MessageHandler.ParseResponse<T>(request, response);
        }

        internal async Task<PaginatedResult<T>> ExecutePaginatedRequest<T>(AblyRequest request, Func<DataRequestQuery, Task<PaginatedResult<T>>> executeDataQueryRequest) where T : class
        {
            var response = await ExecuteRequest(request);
            if (Logger.IsDebug)
            {
                Logger.Debug("Response received. Status: " + response.StatusCode);
                Logger.Debug("Content type: " + response.ContentType);
                Logger.Debug("Encoding: " + response.Encoding);
                if (response.Body != null)
                    Logger.Debug("Raw response (base64):" + response.Body.ToBase64());
            }

            return MessageHandler.ParsePaginatedResponse<T>(request, response, executeDataQueryRequest);
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
        /// Retrieves the stats for the application. Passed default <see cref="StatsDataRequestQuery"/> for the request
        /// </summary>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> StatsAsync()
        {
            return StatsAsync(new StatsDataRequestQuery());
        }

        /// <summary>
        /// Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsDataRequestQuery"/> for more information
        /// </summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> StatsAsync(StatsDataRequestQuery query)
        {
            return StatsAsync(query as DataRequestQuery);
        }

        /// <summary>
        /// Retrieves the stats for the application based on a custom query. It should be used with <see cref="DataRequestQuery"/>.
        /// It is mainly because of the way a PaginatedResource defines its queries. For retrieving Stats with special parameters use <see cref="StatsAsync(IO.Ably.StatsDataRequestQuery)"/>
        /// </summary>
        /// <example>
        /// var client = new AblyRest("validkey");
        /// var stats = client..StatsAsync();
        /// var nextPage = cliest..StatsAsync(stats.NextQuery);
        /// </example>
        /// <param name="query"><see cref="DataRequestQuery"/> and <see cref="StatsDataRequestQuery"/></param>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> StatsAsync(DataRequestQuery query)
        {
            query.Validate();

            var request = CreateGetRequest("/stats");

            request.AddQueryParameters(query.GetParameters());

            return ExecutePaginatedRequest(request, StatsAsync);
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
                return true;

            try
            {
                var request = new AblyRequest(Defaults.InternetCheckURL, HttpMethod.Get);
                var response = await ExecuteHttpRequest(request);
                return response.TextResponse == Defaults.InternetCheckOKMessage;
            }
            catch (Exception ex)
            {
                Logger.Error("Error accessing ably internet check url. Internet is down!", ex);
                return false;
            }
        }
    }
}
