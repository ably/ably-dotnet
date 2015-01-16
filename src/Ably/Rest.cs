using System.Net;
using Ably.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Ably
{
    public class Rest : IAuthCommands, IChannelCommands, IRestCommands
    {
        internal IAblyHttpClient _httpClient;
        private AblyOptions _options;
        private ILogger Logger = Config.AblyLogger;
        internal AuthMethod AuthMethod;
        internal Token CurrentToken;
        internal IResponseHandler ResponseHandler = new ResponseHandler();
        internal IRequestHandler RequestHandler = new RequestHandler();
        private TokenRequest _lastTokenRequest;
        private Protocol _protocol;
        internal Protocol Protocol { get { return _protocol; } }

        internal AblyOptions Options
        {
            get { return _options; }
        }

        public Rest()
        {
            var key = GetConnectionString();
            if (string.IsNullOrEmpty(key))
            {
                throw new AblyException("A connection strig with key 'Ably' doesn't exist in the application configuration");
            }
            
            //TODO: Parse it when I know how things work
        }

        public Rest(string apiKey) : this(new AblyOptions { Key = apiKey})
        {

        }

        public Rest(Action<AblyOptions> init)
        {
            _options = new AblyOptions();

            init(_options);

            InitialiseAbly();
        }

        public Rest(AblyOptions ablyOptions)
        {
            _options = ablyOptions;
            InitialiseAbly();
        }

        internal virtual string GetConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings["Ably"];
            if (connString == null)
            {
                return string.Empty;
            }

            return connString.ConnectionString;
        }

        private void InitialiseAbly()
        {
            ExecuteRequest = ExecuteRequestInternal;
            if(_options == null)
            {
                Logger.Error("No options provider to Ably rest");
                throw new AblyException("Invalid options");
            }

            if(_options.Key.IsNotEmpty())
            {
                var key = ApiKey.Parse(_options.Key);
                _options.AppId = key.AppId;
                _options.KeyId = key.KeyId;
                _options.KeyValue = key.KeyValue;
            }

            if (_options.UseBinaryProtocol == false)
            {
                _protocol = Protocol.Json;
            }
            else
            {
                _protocol = _options.Protocol ?? Protocol.MsgPack;
            }

            string host = GetHost();
            _httpClient = new AblyHttpClient(host, _options.Port, _options.Tls);

            InitAuth();
        }

        private string GetHost()
        {
            if (_options.Host.IsNotEmpty()) return _options.Host;

            if (_options.Environment.HasValue)
                return _options.Environment.Value.ToString().ToLower() + "-" + Config.DefaultHost;

            return Config.DefaultHost;
        }


        private void InitAuth()
        {
            if(Options.Key.IsNotEmpty())
            {
                if(Options.ClientId.IsEmpty())
                {
                    AuthMethod = AuthMethod.Basic;
                    Logger.Info("Using basic authentication.");
                    return;
                }
            }

            AuthMethod = AuthMethod.Token;
            Logger.Info("Using token authentication.");
            if (Options.AuthToken.IsNotEmpty())
            {
                CurrentToken = new Token(Options.AuthToken);
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
            else if (Options.KeyValue.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with client-side signing");
            }
            else if (Options.AuthToken.IsNotEmpty())
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
        public IAuthCommands Auth
        {
            get { return this; }
        }

        public IChannelCommands Channels
        {
            get { return this; }
        }

        internal Func<AblyRequest, AblyResponse> ExecuteRequest;
        

        private AblyResponse ExecuteRequestInternal(AblyRequest request)
        {
            if(request.SkipAuthentication == false)
                AddAuthHeader(request);

            return _httpClient.Execute(request);
        }

        private bool TokenCreatedExternally
        {
            get { return Options.AuthUrl.IsNotEmpty() || Options.AuthCallback != null; }
        }

        private bool HasApiKey
        {
            get { return Options.KeyId.IsNotEmpty() && Options.KeyValue.IsNotEmpty(); }
        }

        private bool HasTokenId
        {
            get { return Options.AuthToken.IsNotEmpty();  }
        }

        public bool TokenRenewable
        {
            get { return TokenCreatedExternally || (HasApiKey && HasTokenId == false); }
        }

        internal void AddAuthHeader(AblyRequest request)
        {
            if(AuthMethod == Ably.AuthMethod.Basic)
            {
                var authInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(Options.Key));
                request.Headers["Authorization"] = "Basic " + authInfo;
            }
            else
            {
                if (CurrentToken == null)
                {
                    CurrentToken = Auth.Authorise(null, null, false);
                }

                if (CurrentToken == null)
                    throw new AblyException("Invalid token credentials", 40100, HttpStatusCode.Unauthorized);

                request.Headers["Authorization"] = "Bearer " + CurrentToken.Id.ToBase64();
            }
        }

        Token IAuthCommands.RequestToken(TokenRequest requestData, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;
            
            var data = requestData ?? new TokenRequest { 
                                                        Id = mergedOptions.KeyId,
                                                        ClientId = Options.ClientId};
            
            if (requestData == null && options == null && _lastTokenRequest != null)
            {
                data = _lastTokenRequest;
            }

            data.Id = data.Id ?? mergedOptions.KeyId;

            _lastTokenRequest = data;

            var request = CreatePostRequest(String.Format("/keys/{0}/requestToken", data.Id));
            request.SkipAuthentication = true;
            TokenRequestPostData postData = null;
            if(mergedOptions.AuthCallback != null)
            {
                var token = mergedOptions.AuthCallback(data);
                if (token != null)
                    return token;
                throw new AblyException("AuthCallback returned an invalid token");
            }
            
            if(mergedOptions.AuthUrl.IsNotEmpty())
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
                if(response.Type != ResponseType.Json)
                    throw new AblyException(new ErrorInfo(string.Format("Content Type {0} is not supported by this client library", response.ContentType), 500));

                var signedData = response.TextResponse;
                var jData = JObject.Parse(signedData);
                if (Token.IsToken(jData))
                    return Token.FromJson(jData);

                postData = JsonConvert.DeserializeObject<TokenRequestPostData>(signedData);
            }
            else
            {
                postData = data.GetPostData(mergedOptions.KeyValue);
            }

            if (mergedOptions.QueryTime)
                postData.timestamp = Time().ToUnixTime().ToString();

            request.PostData = postData;

            var result = ExecuteRequest(request);

            try
            {
                var json = JObject.Parse(result.TextResponse);
                return Token.FromJson((JObject)json["access_token"]);
            }
            catch (JsonException ex)
            {
                throw new AblyException(new ErrorInfo("Invalid json returned", 500), ex);
            }
        }

        Token IAuthCommands.Authorise(TokenRequest request, AuthOptions options, bool force)
        {
            if(CurrentToken != null)
            {
                if(CurrentToken.ExpiresAt > Config.Now())
                {
                    if(force == false)
                        return CurrentToken;
                }
                CurrentToken = null;
            }

            CurrentToken = Auth.RequestToken(request, options);
            return CurrentToken;
        }

        TokenRequestPostData IAuthCommands.CreateTokenRequest(TokenRequest requestData, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;
            
            if (mergedOptions.KeyId == null || mergedOptions.KeyValue == null)
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);
            
            var data = requestData ?? new TokenRequest
            {
                ClientId = Options.ClientId
            };

            data.Id = data.Id ?? mergedOptions.KeyId;

            if (data.Id != mergedOptions.KeyId)
                throw new AblyException("Incompatible keys specified", 40102, HttpStatusCode.Unauthorized);
            
            if (requestData == null && options == null && _lastTokenRequest != null)
            {
                data = _lastTokenRequest;
            }

            data.Id = data.Id ?? mergedOptions.KeyId;

            var postData = data.GetPostData(mergedOptions.KeyValue);
            if (mergedOptions.QueryTime)
                postData.timestamp = Time().ToUnixTime().ToString();

            return postData;
        }



        public DateTime Time()
        {
            var request = CreateGetRequest("/time");
            request.SkipAuthentication = true;
            var response = ExecuteRequest(request);
            if (response.Type != ResponseType.Json)
                throw new AblyException("Invalid response from server", 500, null);

            long serverTime = (long)JArray.Parse(response.TextResponse).First;
            return serverTime.FromUnixTimeInMilliseconds();
        }

        public IPartialResult<Stats> Stats()
        {
            return Stats(new StatsDataRequestQuery());
        }

        public IPartialResult<Stats> Stats(DataRequestQuery query)
        {
            query.Validate();

            var request = CreateGetRequest("/stats");

            request.AddQueryParameters(query.GetParameters());

            var response = ExecuteRequest(request);

            var stats = new PartialResult<Stats>();
            if (response.TextResponse.IsEmpty())
                return stats;

            var json = JToken.Parse(response.TextResponse);
            if(json.HasValues && json.Children().Any())
            {
                stats.AddRange(json.Children().Select(token => token.ToObject<Stats>()));
            }

            stats.NextQuery = DataRequestQuery.GetLinkQuery(response.Headers, "next");
            stats.InitialResultQuery = DataRequestQuery.GetLinkQuery(response.Headers, "first");
            
            return stats;
        }

        internal AblyRequest CreateGetRequest(string path, bool encrypted = false, CipherParams @params = null)
        {
            var request = new AblyRequest(path, HttpMethod.Get, Protocol);
            request.Encrypted = encrypted;
            request.CipherParams = @params;
            return request;
        }

        internal AblyRequest CreatePostRequest(string path, bool encrypted = false, CipherParams @params = null)
        {
            var request = new AblyRequest(path, HttpMethod.Post, Protocol);
            request.Encrypted = encrypted;
            request.CipherParams = @params;
            return request;
        }

        IChannel IChannelCommands.Get(string name)
        {
            return new Channel(this, name, new ResponseHandler(), Options.ChannelDefaults); 
        }

        IChannel IChannelCommands.Get(string name, ChannelOptions options)
        {
            return new Channel(this, name, new ResponseHandler(), options);
        }
    }

    public interface IRestCommands
    {
    }
}