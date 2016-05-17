using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using IO.Ably.MessageEncoders;
using IO.Ably.Rest;
using System.Threading.Tasks;
using IO.Ably.Auth;
using IO.Ably.Transport;

namespace IO.Ably
{
    /// <summary>Client for the ably rest API</summary>
    public sealed class AblyRest : IRestClient
    {
        private readonly object _channelLock = new object();
        internal AblyHttpClient HttpClient { get; private set; }
        internal MessageHandler MessageHandler { get; private set; }

        internal string CustomHost
        {
            get { return HttpClient.CustomHost; }
            set { HttpClient.CustomHost = value;  }
        }

        internal AblyAuth AblyAuth { get; private set; }
        internal List<IChannel> RestChannels { get; private set; } = new List<IChannel>();

        /// <summary>
        /// Authentication methods
        /// </summary>
        public IAuthCommands Auth => AblyAuth;

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
        }

        public IChannelCommands Channels => this;

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
                    await AblyAuth.Authorise(null, new AuthOptions() {Force = true});
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

        /// <summary>/// Retrieves the ably service time/// </summary>
        /// <returns></returns>
        public async Task<DateTimeOffset> Time()
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
        public Task<PaginatedResult<Stats>> Stats()
        {
            return Stats(new StatsDataRequestQuery());
        }

        /// <summary>
        /// Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsDataRequestQuery"/> for more information
        /// </summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        public Task<PaginatedResult<Stats>> Stats(StatsDataRequestQuery query)
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
        public Task<PaginatedResult<Stats>> Stats(DataRequestQuery query)
        {
            query.Validate();

            var request = CreateGetRequest("/stats");

            request.AddQueryParameters(query.GetParameters());

            return ExecuteRequest<PaginatedResult<Stats>>(request);
        }

        internal AblyRequest CreateGetRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Get, Protocol) {ChannelOptions = options};
        }

        internal AblyRequest CreatePostRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Post, Protocol) {ChannelOptions = options};
        }

        IChannel IChannelCommands.this[string name] => Channels.Get(name);

        IChannel IChannelCommands.Get(string name)
        {
            return Channels.Get(name, null);
        }

        IChannel IChannelCommands.Get(string name, ChannelOptions options)
        {
            if (name.IsEmpty())
                throw new ArgumentNullException(nameof(name), "Empty channel name");

            lock (_channelLock)
            {
                var channel = RestChannels.FirstOrDefault(x => x.Name.EqualsTo(name)) as RestChannel;
                if (channel == null)
                {
                    var channelOptions = options ?? Options.ChannelDefaults;
                    channel = new RestChannel(this, name, channelOptions);
                    RestChannels.Add(channel);
                }
                else
                {
                    if (options != null &&
                        Equals(channel.Options, options) == false)
                    {
                        channel.SetOptions(options);
                    }
                }
                return channel;
            }
        }

        bool IChannelCommands.Release(string name)
        {
            var channel = Channels.Get(name);
            if (channel != null)
            {
                lock (_channelLock)
                    return RestChannels.Remove(channel);
            }
            return false;
        }

        public IEnumerator<IChannel> GetEnumerator()
        {
            return RestChannels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
