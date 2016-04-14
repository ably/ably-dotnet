using System;
using System.Net;
using System.Net.Http;
using IO.Ably.Auth;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace IO.Ably
{
    public class AblyAuth : IAuthCommands
    {
        internal AblyAuth(ClientOptions options, AblyRest rest)
        {
            Options = options;
            _rest = rest;

            Initialise();
        }

        internal AuthMethod AuthMethod;
        internal ClientOptions Options { get; }
        private TokenParams _lastTokenRequest;
        private readonly AblyRest _rest;

        //public string CurrentClientId { get; set; }

        public TokenDetails CurrentToken { get; set; }

        public string GetClientId()
        {
            return CurrentToken != null ? CurrentToken.ClientId : Options.GetClientId();
        }

        internal bool HasValidToken()
        {
            if (CurrentToken == null)
                return false;
            var exp = CurrentToken.Expires;
            return (exp == DateTimeOffset.MinValue) || (exp >= DateTimeOffset.UtcNow);
        }

        bool HasTokenId => Options.Token.IsNotEmpty();

        public bool TokenRenewable => TokenCreatedExternally || (HasApiKey && HasTokenId == false);

        bool TokenCreatedExternally => Options.AuthUrl.IsNotEmpty() || Options.AuthCallback != null;

        bool HasApiKey => Options.Key.IsNotEmpty();

        internal void Initialise()
        {
            SetAuthMethod();

            if (AuthMethod == AuthMethod.Basic)
            {
                LogCurrentAuthenticationMethod();
                return;
            }

            Logger.Info("Using token authentication.");
            if (Options.TokenDetails != null)
            {
                CurrentToken = Options.TokenDetails;
            }
            else if (Options.Token.IsNotEmpty())
            {
                CurrentToken = new TokenDetails(Options.Token);
            }
            LogCurrentAuthenticationMethod();
        }

        private void SetAuthMethod()
        {
            if (Options.UseTokenAuth.HasValue)
            {
                //ASK: Should I throw an error if a particular auth is not possible
                AuthMethod = Options.UseTokenAuth.Value ? AuthMethod.Token : AuthMethod.Basic;
            }
            else
            {
                AuthMethod = Options.Method;
            }
        }

        internal async Task AddAuthHeader(AblyRequest request)
        {
            EnsureSecureConnection();

            if (request.Headers.ContainsKey("Authorization"))
                request.Headers.Remove("Authorization");

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
                    CurrentToken = await Authorise(null, null, false);
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

        /// <summary>
        /// Makes a token request. This will make a token request now, even if the library already
	    /// has a valid token. It would typically be used to issue tokens for use by other clients.
        /// </summary>
        /// <param name="tokenParams">The <see cref="TokenRequest"/> data used for the token</param>
        /// <param name="options">Extra <see cref="AuthOptions"/> used for creating a token </param>
        /// <returns>A valid ably token</returns>
        /// <exception cref="AblyException"></exception>
        public async Task<TokenDetails> RequestToken(TokenParams tokenParams = null, AuthOptions options = null)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;
            string keyId = "", keyValue = "";
            if (mergedOptions.Key.IsNotEmpty())
            {
                var key = mergedOptions.ParseKey();
                keyId = key.KeyName;
                keyValue = key.KeySecret;
            }

            //TODO: Implement usage of default token params
            TokenParams @params = tokenParams;

            if (@params == null)
            {
                @params = Options.DefaultTokenParams ?? new TokenParams();
                @params.ClientId = GetClientId(); //Ensure the correct clientId is supplied
            }
            

            if (tokenParams == null && options == null && _lastTokenRequest != null)
            {
                @params = _lastTokenRequest;
            }

            _lastTokenRequest = @params;

            EnsureSecureConnection();

            var request = _rest.CreatePostRequest($"/keys/{keyId}/requestToken");
            request.SkipAuthentication = true;
            TokenRequest postData = null;
            if (mergedOptions.AuthCallback != null)
            {
                var token = await mergedOptions.AuthCallback(@params);
                if (token != null)
                    return token;
                throw new AblyException("AuthCallback returned an invalid token");
            }

            if (mergedOptions.AuthUrl.IsNotEmpty())
            {
                var response = await CallAuthUrl(mergedOptions);

                if (response.Type == ResponseType.Text) //Return token string
                    return new TokenDetails(response.TextResponse);

                var signedData = response.TextResponse;
                var jData = JObject.Parse(signedData);

                //TODO: How do you know if it is a valid token
                if (TokenDetails.IsToken(jData))
                    return jData.ToObject<TokenDetails>();

                postData = JsonConvert.DeserializeObject<TokenRequest>(signedData);

                request.Url = $"/keys/{postData.KeyName}/requestToken";
            }
            else
            {
                if (keyId.IsEmpty() || keyValue.IsEmpty())
                {
                    throw new AblyException("TokenAuth is on but there is no way to generate one");
                }

                postData = new TokenRequest().Populate(@params, keyId, keyValue);
            }

            if (mergedOptions.QueryTime)
                postData.Timestamp = (await _rest.Time()).ToUnixTimeInMilliseconds().ToString();

            request.PostData = postData;

            TokenDetails result = await _rest.ExecuteRequest<TokenDetails>(request);

            if (null == result)
                throw new AblyException(new ErrorInfo("Invalid token response returned", 500));
            return result;
        }

        private async Task<AblyResponse> CallAuthUrl(AuthOptions mergedOptions)
        {
            var url = mergedOptions.AuthUrl;
            var protocol = Options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            var authRequest = new AblyRequest(url.ToString(), mergedOptions.AuthMethod, protocol);

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
            AblyResponse response = await _rest.ExecuteRequest(authRequest);
            if (response.Type == ResponseType.Binary)
                throw new AblyException(
                    new ErrorInfo(
                        string.Format("Content Type {0} is not supported by this client library",
                            response.ContentType), 500));

            return response;
        }

        private void EnsureSecureConnection()
        {
            if (AuthMethod == AuthMethod.Basic && Options.Tls == false)
                throw new InsecureRequestException();
        }

        /// <summary>
        /// Ensure valid auth credentials are present. This may rely in an already-known
        /// and valid token, and will obtain a new token if necessary or explicitly
        /// requested.
        /// Authorisation will use the parameters supplied on construction except
        /// where overridden with the options supplied in the call.
        /// </summary>
        /// <param name="tokenParams"><see cref="TokenParams"/> custom parameter. Pass null and default token request options will be generated used the options passed when creating the client</param>
        /// <param name="options"><see cref="AuthOptions"/> custom options.</param>
        /// <param name="force">Force the client request a new token even if it has a valid one.</param>
        /// <returns>Returns a valid token</returns>
        /// <exception cref="AblyException">Throws an ably exception representing the server response</exception>
        public async Task<TokenDetails> Authorise(TokenParams tokenParams, AuthOptions options, bool force)
        {
            if (CurrentToken != null)
            {
                if (CurrentToken.Expires > (Config.Now().AddSeconds(Defaults.TokenExpireBufer)))
                {
                    if (force == false)
                        return CurrentToken;
                }
                CurrentToken = null;
            }

            CurrentToken = await RequestToken(tokenParams, options);
            return CurrentToken;
        }

        /// <summary>
        /// Create a signed token request based on known credentials
        /// and the given token params. This would typically be used if creating
        /// signed requests for submission by another client.
        /// </summary>
        /// <param name="tokenParams"><see cref="TokenParams"/>. If null a token request is generated from options passed when the client was created.</param>
        /// <param name="options"><see cref="AuthOptions"/>. If null the default AuthOptions are used.</param>
        /// <returns></returns>
        public async Task<TokenRequest> CreateTokenRequest(TokenParams tokenParams, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;

            if (string.IsNullOrEmpty(mergedOptions.Key))
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);

            var data = tokenParams ?? new TokenParams
            {
                ClientId = Options.ClientId
            };

            if (tokenParams == null && options == null && _lastTokenRequest != null)
            {
                data = _lastTokenRequest;
            }

            ApiKey key = mergedOptions.ParseKey();
            var request = new TokenRequest().Populate(data, key.KeyName, key.KeySecret);

            if (mergedOptions.QueryTime)
                request.Timestamp = (await _rest.Time()).ToUnixTimeInMilliseconds().ToString();

            return request;
        }


        internal TokenAuthMethod GetTokenAuthMethod()
        {
            if (null != Options.AuthCallback)
                return TokenAuthMethod.Callback;
            if (Options.AuthUrl.IsNotEmpty())
                return TokenAuthMethod.Url;
            if (Options.Key.IsNotEmpty())
                return TokenAuthMethod.Signing;
            if (Options.Token.IsNotEmpty())
                return TokenAuthMethod.JustToken;
            return TokenAuthMethod.None;
        }

        private void LogCurrentAuthenticationMethod()
        {
            TokenAuthMethod method = GetTokenAuthMethod();
            Logger.Info("Authentication method: {0}", method.ToEnumDescription());
        }
    }
}