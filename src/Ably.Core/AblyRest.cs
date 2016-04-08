using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using System.Threading.Tasks;

namespace IO.Ably
{
    /// <summary>Client for the ably rest API</summary>
    public sealed class AblyRest : AblyBase, IRestClient, IAblyRest
    {
        internal IAblyHttpClient _httpClient;
        internal MessageHandler _messageHandler;

        /// <summary>Initializes the RestClient by reading the Key from a connection string with key 'Ably'</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public AblyRest()
        {
            var key = Platform.IoC.GetConnectionString();
            if( string.IsNullOrEmpty( key ) )
                throw new AblyException( "A connection string with key 'Ably' doesn't exist in the application configuration" );
            _options = new ClientOptions( key );
            InitializeAbly();
        }

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
            _options = new ClientOptions();
            init(_options);
            InitializeAbly();
        }

        /// <summary>
        /// Initialize the library with a custom set of options
        /// </summary>
        /// <param name="clientOptions"></param>
        public AblyRest(ClientOptions clientOptions)
        {
            _options = clientOptions;
            InitializeAbly();
        }

        /// <summary>Initializes the rest client and validates the passed in options</summary>
        private void InitializeAbly()
        {
            if (_options == null)
            {
                Logger.Error("No options provider to Ably rest");
                throw new AblyException("Invalid options");
            }

            _protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            Logger.Debug("Protocol set to: " + _protocol);
            _messageHandler = new MessageHandler(_protocol);

            string host = GetHost();
            _httpClient = new AblyHttpClient(host, _options.Port, _options.Tls, _options.Environment);
            ExecuteHttpRequest = _httpClient.Execute;

            InitAuth(this);
        }


        string GetHost()
        {
            if (_options.RestHost.IsNotEmpty())
                return _options.RestHost;

            return Config.DefaultHost;
        }

        /// <summary>
        /// Channel methods
        /// </summary>
        public IChannelCommands Channels
        {
            get { return this; }
        }

        internal IAblyRest RestMethods
        {
            get { return this;}
        }

        internal Func<AblyRequest, Task<AblyResponse>> ExecuteHttpRequest;

        async Task<AblyResponse> IAblyRest.ExecuteRequest(AblyRequest request)
        {
            Logger.Info("Sending {0} request to {1}", request.Method, request.Url);

            if (request.SkipAuthentication == false)
                await AddAuthHeader(request);

            _messageHandler.SetRequestBody(request);

            return await ExecuteHttpRequest(request);
        }

        async Task<T> IAblyRest.ExecuteRequest<T>(AblyRequest request)
        {
            var response = await RestMethods.ExecuteRequest(request);
            Logger.Debug("Response received. Status: " + response.StatusCode);
            Logger.Debug("Content type: " + response.ContentType);
            Logger.Debug("Encoding: " + response.Encoding);
            if(response.Body != null)
                Logger.Debug("Raw response (base64):" + response.Body.ToBase64());

            return _messageHandler.ParseResponse<T>(request, response);
        }

        bool TokenCreatedExternally
        {
            get { return StringExtensions.IsNotEmpty(Options.AuthUrl) || Options.AuthCallback != null; }
        }

        bool HasApiKey
        {
            get { return StringExtensions.IsNotEmpty(Options.Key); }
        }

        bool HasTokenId
        {
            get { return StringExtensions.IsNotEmpty(Options.Token); }
        }

        public bool TokenRenewable
        {
            get { return TokenCreatedExternally || (HasApiKey && HasTokenId == false); }
        }

        internal async Task AddAuthHeader(AblyRequest request)
        {
            if (AuthMethod == AuthMethod.Basic)
            {
                var authInfo = Convert.ToBase64String(Options.Key.GetBytes());
                request.Headers["Authorization"] = "Basic " + authInfo;
                Logger.Debug("Adding Authorization header with Basic authentication.");
            }
            else
            {
                if (HasValidToken() == false && TokenRenewable)
                {
                    CurrentToken = await Auth.Authorise(null, null, false);
                }

                if (HasValidToken())
                {
                    request.Headers["Authorization"] = "Bearer " + CurrentToken.Token.ToBase64();
                    Logger.Debug("Adding Authorization header with Token authentication");
                }
                else
                    throw new AblyException("Invalid token credentials: " + CurrentToken, 40100, HttpStatusCode.Unauthorized);
            }
        }

        /// <summary>/// Retrieves the ably service time/// </summary>
        /// <returns></returns>
        public async Task<DateTime> Time()
        {
            AblyRequest request = RestMethods.CreateGetRequest("/time");
            request.SkipAuthentication = true;
            List<long> response = await RestMethods.ExecuteRequest<List<long>>(request);
            return response.First().FromUnixTimeInMilliseconds();
        }

        /// <summary>
        /// Retrieves the stats for the application. Passed default <see cref="StatsDataRequestQuery"/> for the request
        /// </summary>
        /// <returns></returns>
        public Task<PaginatedResource<Stats>> Stats()
        {
            return Stats(new StatsDataRequestQuery());
        }

        /// <summary>
        /// Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsDataRequestQuery"/> for more information
        /// </summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        public Task<PaginatedResource<Stats>> Stats(StatsDataRequestQuery query)
        {
            return Stats(query as DataRequestQuery);
        }

        /// <summary>
        /// Retrieves the stats for the application based on a custom query. It should be used with <see cref="DataRequestQuery"/>.
        /// It is mainly because of the way a PaginatedResource defines its queries. For retrieving Stats with special parameters use <see cref="AblyRest.Stats(StatsDataRequestQuery query)"/>
        /// </summary>
        /// <example>
        /// var client = new AblyRest("validkey");
        /// var stats = client.Stats();
        /// var nextPage = cliest.Stats(stats.NextQuery);
        /// </example>
        /// <param name="query"><see cref="DataRequestQuery"/> and <see cref="StatsDataRequestQuery"/></param>
        /// <returns></returns>
        public Task<PaginatedResource<Stats>> Stats(DataRequestQuery query)
        {
            query.Validate();

            var request = RestMethods.CreateGetRequest("/stats");

            request.AddQueryParameters(query.GetParameters());

            return RestMethods.ExecuteRequest<PaginatedResource<Stats>>(request);
        }

        AblyRequest IAblyRest.CreateGetRequest(string path, ChannelOptions options)
        {
            return new AblyRequest(path, HttpMethod.Get, Protocol) { ChannelOptions = options };
        }

        AblyRequest IAblyRest.CreatePostRequest(string path, ChannelOptions options)
        {
            return new AblyRequest(path, HttpMethod.Post, Protocol) { ChannelOptions = options };
        }

        IChannel IChannelCommands.this[string name]
        {
            get { return ((IChannelCommands)this).Get(name); }
        }

        IChannel IChannelCommands.Get(string name)
        {
            return new Channel(this, name, Options.ChannelDefaults);
        }

        IChannel IChannelCommands.Get(string name, ChannelOptions options)
        {
            return new Channel(this, name, options);
        }
    }
}
