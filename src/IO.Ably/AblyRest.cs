using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using System.Threading.Tasks;
using IO.Ably.Auth;

namespace IO.Ably
{
    /// <summary>Client for the ably rest API</summary>
    public sealed class AblyRest : AblyBase, IRestClient, IAblyRest
    {
        internal AblyHttpClient HttpClient;
        internal MessageHandler MessageHandler;

        internal AblyAuth AblyAuth { get; private set; }

        /// <summary>
        /// Authentication methods
        /// </summary>
        public IAuthCommands Auth => AblyAuth;

        internal Protocol Protocol { get; private set; }

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

            Protocol = Options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            Logger.Debug("Protocol set to: " + Protocol);
            MessageHandler = new MessageHandler(Protocol);

            var port = Options.Tls ? Options.TlsPort : Options.Port;
            HttpClient = new AblyHttpClient(Options.RestHost, port, Options.Tls, Options.Environment);
            ExecuteHttpRequest = HttpClient.Execute;

            AblyAuth = new AblyAuth(Options, this);
        }

        public IChannelCommands Channels => this;

        internal IAblyRest RestMethods => this;

        internal Func<AblyRequest, Task<AblyResponse>> ExecuteHttpRequest;
          
        async Task<AblyResponse> IAblyRest.ExecuteRequest(AblyRequest request)
        {
            Logger.Info("Sending {0} request to {1}", request.Method, request.Url);

            if (request.SkipAuthentication == false)
                await AblyAuth.AddAuthHeader(request);

            MessageHandler.SetRequestBody(request);

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

            return MessageHandler.ParseResponse<T>(request, response);
        }

        /// <summary>/// Retrieves the ably service time/// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> Time()
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
            return new RestChannel(this, name, Options.ChannelDefaults);
        }

        IChannel IChannelCommands.Get(string name, ChannelOptions options)
        {
            return new RestChannel(this, name, options);
        }
    }
}
