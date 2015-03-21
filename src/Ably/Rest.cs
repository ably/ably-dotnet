using System.Net;
using Ably.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Ably
{
    public static class Config
    {
        public static ILogger AblyLogger = Logger.Current;
        public static Func<CipherParams, IChannelCipher> GetCipher = @params => new AesCipher(@params);
        internal static string DefaultHost = "rest.ably.io";
        internal static Func<DateTime> Now = () => DateTime.Now;

    }

    public class Rest : IAuthCommands, IChannelCommands<IChannel>, IRestCommands
    {
        internal IAblyHttpClient _client;
        private AblyOptions _options;
        private ILogger Logger = Config.AblyLogger;
        internal AuthMethod AuthMethod;
        internal Token CurrentToken;
        internal IResponseHandler ResponseHandler = new ResponseHandler();
        internal IRequestHandler RequestHandler = new RequestHandler();


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
            
            //Parse it when I know how things work
        }

        public Rest(string apiKey) : this(new AblyOptions(apiKey))
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

            if(_options.AppId.IsEmpty())
            {
                Logger.Error("Cannot initialise Ably without AppId");
                throw new AblyException("Cannot initialise Ably without an AppId");
            }

            string host = _options.Host.IsNotEmpty() ? _options.Host : Config.DefaultHost;
            _client = new AblyHttpClient(host, _options.Port, _options.Tls);

            InitAuth();
        }

        
        private void InitAuth()
        {
            if(Options.Key.IsNotEmpty())
            {
                if(Options.ClientId == null)
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
                CurrentToken = new Token() { Id = Options.AuthToken };
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

        public IChannelCommands<IChannel> Channels
        {
            get { return this; }
        }

        internal Func<AblyRequest, AblyResponse> ExecuteRequest;

        private AblyResponse ExecuteRequestInternal(AblyRequest request)
        {
            if(request.SkipAuthentication == false)
                AddAuthHeader(request);


            return _client.Execute(request);
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
                                                        ClientId = Options.ClientId,
                                                        Capability = new Capability() };

            data.Id = data.Id ?? mergedOptions.KeyId;
            data.Capability = data.Capability ?? new Capability();
            data.Validate();

            var request = CreatePostRequest(String.Format("/keys/{0}/requestToken", data.Id));
            request.SkipAuthentication = true;
            TokenRequestPostData postData = null;
            if(mergedOptions.AuthCallback != null)
            {
                var signedPostData = mergedOptions.AuthCallback(data);
                postData = JsonConvert.DeserializeObject<TokenRequestPostData>(signedPostData);
            }
            else if(mergedOptions.AuthUrl.IsNotEmpty())
            {
                var authRequest = new AblyRequest(mergedOptions.AuthUrl, HttpMethod.Post);
                authRequest.PostParameters.Merge(mergedOptions.AuthParams);
                authRequest.Headers.Merge(mergedOptions.AuthHeaders);
                authRequest.SkipAuthentication = true;
                var response = ExecuteRequest(authRequest);
                var signedData = response.TextResponse;
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
                return Token.fromJSON(JObject.Parse(result.TextResponse));
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
                if(CurrentToken.Expires > Config.Now())
                {
                    if(force == false)
                        return CurrentToken;
                }
                CurrentToken = null;
            }

            CurrentToken = Auth.RequestToken(request, options);
            return CurrentToken;
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
            var request = new AblyRequest(path, HttpMethod.Get);
            request.UseTextProtocol = _options.UseTextProtocol;
            request.Encrypted = encrypted;
            request.CipherParams = @params;
            return request;
        }

        internal AblyRequest CreatePostRequest(string path, bool encrypted = false, CipherParams @params = null)
        {
            var request = new AblyRequest(path, HttpMethod.Post);
            request.UseTextProtocol = _options.UseTextProtocol;
            request.Encrypted = encrypted;
            request.CipherParams = @params;
            return request;
        }

        IChannel IChannelCommands<IChannel>.Get(string name)
        {
            return new Channel(this, name, new ResponseHandler(), Options.ChannelDefaults); 
        }

        IChannel IChannelCommands<IChannel>.Get(string name, ChannelOptions options)
        {
            return new Channel(this, name, new ResponseHandler(), options);
        }
    }

    public interface IRestCommands
    {
    }
}