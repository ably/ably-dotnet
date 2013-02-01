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
        internal static string DefaultHost = "rest.ably.io";
        internal static Func<DateTime> Now = () => DateTime.Now;
    }

    public interface IAuthCommands
    {
        Token RequestToken(TokenRequest request, AuthOptions options);
        Token Authorise(TokenRequest request, AuthOptions options, bool force);
    }

    public interface IChannelCommands
    {
        IChannel Get(string name);
    }

    public class Rest : IAuthCommands, IChannelCommands
    {
        internal IAblyHttpClient _client;
        private AblyOptions _options;
        private ILogger Logger = Config.AblyLogger;
        internal AuthMethod AuthMethod;
        internal Token CurrentToken;

        internal static readonly MimeTypes MimeTypes = new MimeTypes();

        internal AblyOptions Options
        {
            get { return _options; }
        }

        public Rest()
        {
            var key = GetConnectionString();
            if (string.IsNullOrEmpty(key))
            {
                new ConfigurationMissingException("A connection strig with key 'Ably' doesn't exist in the application configuration").Throw();
            }
            
            //Parse it when I know how things work
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
                new ArgumentNullException("Options").Throw();
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
                new ArgumentException("Cannot initialise Ably without an AppId").Throw();
            }

            string host = _options.Host.IsNotEmpty() ? _options.Host : Config.DefaultHost;
            _client = new AblyHttpClient(host, _options.Port, _options.Encrypted);

            InitAuth();
        }

        
        private void InitAuth()
        {
            if(Options.Key.IsNotEmpty())
            {
                if(Options.ClientId == null)
                {
                    AuthMethod = AuthMethod.Basic;
                    Logger.Info("Using basic authentication for all calls");
                    return;
                }
            }

            AuthMethod = AuthMethod.Token;
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
            else if (Options.KeyValue.IsNotEmpty() != null)
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
            return _client.Execute(request);
        }

        private void AddAuthHeader(AblyRequest request)
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
                    throw new AblyException("Invalid token credentials");

                request.Headers["Authorization"] = "Bearer " + CurrentToken.Id;
            }
        }

        Token IAuthCommands.RequestToken(TokenRequest requestData, AuthOptions options)
        {
            var data = requestData ?? new TokenRequest { 
                                                        Id = Options.KeyId,
                                                        ClientId = Options.ClientId,
                                                        Capability = new Capability() };
            data.Id = data.Id ?? Options.KeyId;
            data.Capability = data.Capability ?? new Capability();
            data.Validate();

            var mergedOptions = options != null ? options.Merge(Options) : Options;

            var request = CreatePostRequest(String.Format("/apps/{0}/authorise", Options.AppId));
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
                var signedData = response.JsonResult;
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
                return Token.fromJSON(JObject.Parse(result.JsonResult));
            }
            catch (JsonException ex)
            {
                throw new AblyException("Invalid json returned", ex);
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
                throw new AblyException("Invalid response from server");

            long serverTime = (long)JArray.Parse(response.JsonResult).First;
            return serverTime.FromUnixTimeInMilliseconds();
        }

        public Stats Stats()
        {
            return Stats(new DataRequestQuery());
        }

        public Stats Stats(DataRequestQuery query)
        {
            query.Validate();

            var request = CreateGetRequest("/apps/" + Options.AppId + "/stats");

            if (query.Start.HasValue)
                request.QueryParameters.Add("start", query.Start.Value.ToUnixTime().ToString());

            if (query.End.HasValue)
                request.QueryParameters.Add("end", query.End.Value.ToUnixTime().ToString());

            request.QueryParameters.Add("direction", query.Direction.ToString().ToLower());
            if (query.Limit.HasValue)
                request.QueryParameters.Add("limit", query.Limit.Value.ToString());

            ExecuteRequest(request);

            return new Stats();
        }

        public IEnumerable<Message> History()
        {
            return History(new DataRequestQuery());
        }

        public IEnumerable<Message> History(DataRequestQuery query)
        {
            query.Validate();

            var request = CreateGetRequest("/apps/" + Options.AppId + "/history");

            if (query.Start.HasValue)
                request.QueryParameters.Add("start", query.Start.Value.ToUnixTime().ToString());

            if (query.End.HasValue)
                request.QueryParameters.Add("end", query.End.Value.ToUnixTime().ToString());

            request.QueryParameters.Add("direction", query.Direction.ToString().ToLower());
            if (query.Limit.HasValue)
                request.QueryParameters.Add("limit", query.Limit.Value.ToString());

            ExecuteRequest(request);
            return new List<Message>();
        }

        internal AblyRequest CreateGetRequest(string path)
        {
            var request = new AblyRequest(path, HttpMethod.Get);
            foreach(var header in GetDefaultHeaders(_options.UseTextProtocol == false))
            {
                request.Headers.Add(header.Key, header.Value);
            }
            return request;
        }

        internal AblyRequest CreatePostRequest(string path)
        {
            var request = new AblyRequest(path, HttpMethod.Post);
            foreach (var header in GetDefaultPostHeaders(_options.UseTextProtocol == false))
            {
                request.Headers.Add(header.Key, header.Value);
            }
            return request;
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultHeaders(bool binary)
        {
            if (binary)
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("binary", "json"));
            }
            else
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("json"));
            }
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultPostHeaders(bool binary)
        {
            if (binary)
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("binary", "json"));
                yield return new KeyValuePair<string, string>("Content-Type", MimeTypes.GetHeaderValue("binary"));
            }
            else
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("json"));
                yield return new KeyValuePair<string, string>("Content-Type", MimeTypes.GetHeaderValue("json"));
            }
        }

        IChannel IChannelCommands.Get(string name)
        {
            return new Channel(this, name); 
        }
    }
}