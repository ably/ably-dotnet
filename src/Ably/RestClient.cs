using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using Ably.Auth;
using Ably.CustomSerialisers;
using Ably.MessageEncoders;
using Ably.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ably
{
    /// <summary>
    /// Client for the ably rest API
    /// </summary>
    public sealed class RestClient : IRestClient
    {
        static RestClient()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new DateTimeOffsetJsonConverter(),
                    new CapabilityJsonConverter()
                }
            };
        }


        internal IAblyHttpClient _httpClient;
        private AblyOptions _options;
        private readonly ILogger Logger = Config.AblyLogger;
        internal AuthMethod AuthMethod;
        internal TokenDetails CurrentToken;
        internal MessageHandler _messageHandler;
        private TokenRequest _lastTokenRequest;
        private Protocol _protocol;

        internal Protocol Protocol
        {
            get { return _protocol; }
        }

        internal AblyOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Initialises the RestClient by reading the Key from a connection string with key 'Ably'
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RestClient()
        {
            var key = GetConnectionString();
            if (string.IsNullOrEmpty(key))
            {
                throw new AblyException(
                    "A connection string with key 'Ably' doesn't exist in the application configuration");
            }

            _options = new AblyOptions(key);
            InitialiseAbly();
        }

        /// <summary>
        /// Initialises the RestClient using the api key provided
        /// </summary>
        /// <param name="apiKey">Full api key</param>
        public RestClient(string apiKey)
            : this(new AblyOptions(apiKey))
        {

        }

        /// <summary>
        /// Convenience method for initialising the RestClient by passing a Action{AblyOptions} 
        /// <example>
        /// var rest = new RestClient(opt => {
        ///  opt.Key = "fake.key:value";
        ///  opt.ClientId = "123";
        /// });
        /// </example>
        /// </summary>
        /// <param name="init">Action delegate which receives a empty options object.</param>
        public RestClient(Action<AblyOptions> init)
        {
            _options = new AblyOptions();
            init(_options);
            InitialiseAbly();
        }

        /// <summary>
        /// Initialise the library with a custom set of options
        /// </summary>
        /// <param name="ablyOptions"></param>
        public RestClient(AblyOptions ablyOptions)
        {
            _options = ablyOptions;
            InitialiseAbly();
        }


        /// <summary>
        /// Retrieves the ably connection string from app.config / web.config
        /// </summary>
        /// <returns>Ably connections string. Empty if connection string does not exist.</returns>
        internal string GetConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings["Ably"];
            if (connString == null)
            {
                return string.Empty;
            }

            return connString.ConnectionString;
        }

        /// <summary>
        /// Initialises the rest client and validates the passed in options
        /// </summary>
        private void InitialiseAbly()
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

            InitAuth();
        }


        private string GetHost()
        {
            if (_options.Host.IsNotEmpty()) 
                return _options.Host;

            return Config.DefaultHost;
        }

        /// <summary>
        /// Initialises the Ably Auth type based on the options passed.
        /// </summary>
        private void InitAuth()
        {
            if (Options.Key.IsNotEmpty())
            {
                if (Options.ClientId.IsEmpty())
                {
                    AuthMethod = AuthMethod.Basic;
                    Logger.Info("Using basic authentication.");
                    return;
                }
            }

            AuthMethod = AuthMethod.Token;
            Logger.Info("Using token authentication.");
            if (Options.Token.IsNotEmpty())
            {
                CurrentToken = new TokenDetails(Options.Token);
            }
            LogCurrentAuthenticationMethod();
        }


        private void LogCurrentAuthenticationMethod()
        {
            if (Options.AuthCallback != null)
            {
                Logger.Info("Authentication will be done using token auth with authCallback");
            }
            else if (Options.AuthUrl.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with authUrl");
            }
            else if (Options.Key.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with client-side signing");
            }
            else if (Options.Token.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with supplied token only");
            }
            else
            {
                /* this is not a hard error - but any operation that requires
                 * authentication will fail */
                Logger.Info("Authentication will fail because no authentication parameters supplied");
            }
        }

        /// <summary>
        /// Authentication methods
        /// </summary>
        public IAuthCommands Auth
        {
            get { return this; }
        }

        /// <summary>
        /// Channel methods
        /// </summary>
        public IChannelCommands<IChannel> Channels
        {
            get { return this; }
        }

        internal Func<AblyRequest, AblyResponse> ExecuteHttpRequest;

        internal AblyResponse ExecuteRequest(AblyRequest request)
        {
            Logger.Info("Sending {0} request to {1}", request.Method, request.Url);
            
            if (request.SkipAuthentication == false)
                AddAuthHeader(request);

            _messageHandler.SetRequestBody(request);

            return ExecuteHttpRequest(request);
        }

        internal T ExecuteRequest<T>(AblyRequest request) where T : class
        {
            var response = ExecuteRequest(request);
            Logger.Debug("Response received. Status: " + response.StatusCode);
            Logger.Debug("Content type: " + response.ContentType);
            Logger.Debug("Encoding: " + response.Encoding);
            if(response.Body != null)
                Logger.Debug("Raw response (base64):" + response.Body.ToBase64());

            return _messageHandler.ParseResponse<T>(request, response);
        }

        private bool TokenCreatedExternally
        {
            get { return Options.AuthUrl.IsNotEmpty() || Options.AuthCallback != null; }
        }

        private bool HasApiKey
        {
            get { return Options.Key.IsNotEmpty(); }
        }

        private bool HasTokenId
        {
            get { return Options.Token.IsNotEmpty(); }
        }

        public bool TokenRenewable
        {
            get { return TokenCreatedExternally || (HasApiKey && HasTokenId == false); }
        }

        internal void AddAuthHeader(AblyRequest request)
        {
            if (AuthMethod == AuthMethod.Basic)
            {
                var authInfo = Convert.ToBase64String(Options.Key.GetBytes());
                request.Headers["Authorization"] = "Basic " + authInfo;
                Logger.Debug("Adding Authorisation header with Basic authentication.");
            }
            else
            {
                if (HasValidToken() == false && TokenRenewable)
                {
                    CurrentToken = Auth.Authorise(null, null, false);
                }

                if (HasValidToken())
                {
                    request.Headers["Authorization"] = "Bearer " + CurrentToken.Token.ToBase64();
                    Logger.Debug("Adding Authorization headir with Token authentication");
                }
                else
                    throw new AblyException("Invalid token credentials: " + CurrentToken, 40100, HttpStatusCode.Unauthorized);
            }
        }

        private bool HasValidToken()
        {
            return CurrentToken != null &&
                   (CurrentToken.Expires == DateTimeOffset.MinValue || CurrentToken.Expires >= DateTimeOffset.UtcNow);
        }


        /// <summary>
        /// Makes a token request. This will make a token request now, even if the library already
	    /// has a valid token. It would typically be used to issue tokens for use by other clients.
        /// </summary>
        /// <param name="requestData">The <see cref="TokenRequest"/> data used for the token</param>
        /// <param name="options">Extra <see cref="AuthOptions"/> used for creating a token </param>
        /// <returns>A valid ably token</returns>
        /// <exception cref="AblyException"></exception>
        TokenDetails IAuthCommands.RequestToken(TokenRequest requestData, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;
            string keyId = "", keyValue = "";
            if (!string.IsNullOrEmpty(mergedOptions.Key))
            {
                var key = mergedOptions.ParseKey();
                keyId = key.KeyId;
                keyValue = key.KeyValue;
            }

            var data = requestData ?? new TokenRequest
            {
                KeyName = keyId,
                ClientId = Options.ClientId
            };

            if (requestData == null && options == null && _lastTokenRequest != null)
            {
                data = _lastTokenRequest;
            }

            data.KeyName = data.KeyName ?? keyId;

            _lastTokenRequest = data;

            var request = CreatePostRequest(String.Format("/keys/{0}/requestToken", data.KeyName));
            request.SkipAuthentication = true;
            TokenRequestPostData postData = null;
            if (mergedOptions.AuthCallback != null)
            {
                var token = mergedOptions.AuthCallback(data);
                if (token != null)
                    return token;
                throw new AblyException("AuthCallback returned an invalid token");
            }

            if (mergedOptions.AuthUrl.IsNotEmpty())
            {
                var url = mergedOptions.AuthUrl;
                var authRequest = new AblyRequest(url, mergedOptions.AuthMethod, Protocol);
                if (mergedOptions.AuthMethod == HttpMethod.Get)
                {
                    authRequest.AddQueryParameters(mergedOptions.AuthParams);
                }
                else
                {
                    authRequest.PostParameters = mergedOptions.AuthParams;
                }
                authRequest.Headers.Merge(mergedOptions.AuthHeaders);
                authRequest.SkipAuthentication = true;
                var response = ExecuteRequest(authRequest);
                if (response.Type != ResponseType.Json)
                    throw new AblyException(
                        new ErrorInfo(
                            string.Format("Content Type {0} is not supported by this client library",
                                response.ContentType), 500));

                var signedData = response.TextResponse;
                var jData = JObject.Parse(signedData);
                if (TokenDetails.IsToken(jData))
                    return jData.ToObject<TokenDetails>();

                postData = JsonConvert.DeserializeObject<TokenRequestPostData>(signedData);
            }
            else
            {
                postData = data.GetPostData(keyValue);
            }

            if (mergedOptions.QueryTime)
                postData.timestamp = Time().ToUnixTime().ToString();

            request.PostData = postData;

            var result = ExecuteRequest<TokenResponse>(request);

            if (result == null || result.AccessToken == null)
                throw new AblyException(new ErrorInfo("Invalid token response returned", 500));

            return result.AccessToken;
        }

        /// <summary>
        /// Ensure valid auth credentials are present. This may rely in an already-known
        /// and valid token, and will obtain a new token if necessary or explicitly
        /// requested.
        /// Authorisation will use the parameters supplied on construction except
        /// where overridden with the options supplied in the call.
        /// </summary>
        /// <param name="request"><see cref="TokenRequest"/> custom parameter. Pass null and default token request options will be generated used the options passed when creating the client</param>
        /// <param name="options"><see cref="AuthOptions"/> custom options.</param>
        /// <param name="force">Force the client request a new token even if it has a valid one.</param>
        /// <returns>Returns a valid token</returns>
        /// <exception cref="AblyException">Throws an ably exception representing the server response</exception>
        TokenDetails IAuthCommands.Authorise(TokenRequest request, AuthOptions options, bool force)
        {
            if (CurrentToken != null)
            {
                if (CurrentToken.Expires > Config.Now())
                {
                    if (force == false)
                        return CurrentToken;
                }
                CurrentToken = null;
            }

            CurrentToken = Auth.RequestToken(request, options);
            return CurrentToken;
        }

        /// <summary>
        /// Create a signed token request based on known credentials
        /// and the given token params. This would typically be used if creating
        /// signed requests for submission by another client.
        /// </summary>
        /// <param name="requestData"><see cref="TokenRequest"/>. If null a token request is generated from options passed when the client was created.</param>
        /// <param name="options"><see cref="AuthOptions"/>. If null the default AuthOptions are used.</param>
        /// <returns></returns>
        TokenRequestPostData IAuthCommands.CreateTokenRequest(TokenRequest requestData, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;

            if (string.IsNullOrEmpty(mergedOptions.Key))
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);

            var data = requestData ?? new TokenRequest
            {
                ClientId = Options.ClientId
            };

            ApiKey key = mergedOptions.ParseKey();
            data.KeyName = data.KeyName ?? key.KeyId;

            if (data.KeyName != key.KeyId)
                throw new AblyException("Incompatible keys specified", 40102, HttpStatusCode.Unauthorized);

            if (requestData == null && options == null && _lastTokenRequest != null)
            {
                data = _lastTokenRequest;
            }

            data.KeyName = data.KeyName ?? key.KeyId;

            var postData = data.GetPostData(key.KeyValue);
            if (mergedOptions.QueryTime)
                postData.timestamp = Time().ToUnixTime().ToString();

            return postData;
        }

        /// <summary>
        /// Retrieves the ably service time
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset Time()
        {
            var request = CreateGetRequest("/time");
            request.SkipAuthentication = true;
            var response = ExecuteRequest<List<long>>(request);

            return response.First().FromUnixTimeInMilliseconds();
        }

        /// <summary>
        /// Retrieves the stats for the application. Passed default <see cref="StatsDataRequestQuery"/> for the request
        /// </summary>
        /// <returns></returns>
        public IPaginatedResource<Stats> Stats()
        {
            return Stats(new StatsDataRequestQuery());
        }

        /// <summary>
        /// Retrieves the stats for the application using a more specific stats query. Check <see cref="StatsDataRequestQuery"/> for more information
        /// </summary>
        /// <param name="query">stats query</param>
        /// <returns></returns>
        public IPaginatedResource<Stats> Stats(StatsDataRequestQuery query)
        {
            return Stats(query as DataRequestQuery);
        }

        /// <summary>
        /// Retrieves the stats for the application based on a custom query. It should be used with <see cref="DataRequestQuery"/>.
        /// It is mainly because of the way a PaginatedResource defines its queries. For retrieving Stats with special parameters use <see cref="RestClient.Stats(StatsDataRequestQuery query)"/>
        /// </summary>
        /// <example>
        /// var client = new RestClient("validkey");
        /// var stats = client.Stats();
        /// var nextPage = cliest.Stats(stats.NextQuery);
        /// </example>
        /// <param name="query"><see cref="DataRequestQuery"/> and <see cref="StatsDataRequestQuery"/></param>
        /// <returns></returns>
        public IPaginatedResource<Stats> Stats(DataRequestQuery query)
        {
            query.Validate();

            var request = CreateGetRequest("/stats");

            request.AddQueryParameters(query.GetParameters());

            return ExecuteRequest<PaginatedResource<Stats>>(request);
        }

        internal AblyRequest CreateGetRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Get, Protocol) { ChannelOptions = options };
        }

        internal AblyRequest CreatePostRequest(string path, ChannelOptions options = null)
        {
            return new AblyRequest(path, HttpMethod.Post, Protocol) { ChannelOptions = options };
        }

        IChannel IChannelCommands<IChannel>.this[string name]
        {
            get { return ((IChannelCommands<IChannel>)this).Get(name); }
        }

        IChannel IChannelCommands<IChannel>.Get(string name)
        {
            return new Channel(this, name, Options.ChannelDefaults);
        }

        IChannel IChannelCommands<IChannel>.Get(string name, ChannelOptions options)
        {
            return new Channel(this, name, options);
        }
    }
}