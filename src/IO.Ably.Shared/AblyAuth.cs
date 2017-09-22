using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using IO.Ably;

namespace IO.Ably
{
    internal class AblyAuth : IAblyAuth
    {
        internal AblyAuth(ClientOptions options, AblyRest rest)
        {
            Now = options.NowFunc;
            Options = options;
            _rest = rest;
            Logger = options.Logger;
            Initialise();
        }
        internal Func<DateTimeOffset> Now { get; set; }
        internal ILogger Logger { get; private set; }

        public AuthMethod AuthMethod { get; private set; }
        internal ClientOptions Options { get; }
        internal TokenParams CurrentTokenParams { get; set; }
        internal AuthOptions CurrentAuthOptions { get; set; }
        private readonly AblyRest _rest;

        public TokenDetails CurrentToken { get; set; }

        public void ExpireCurrentToken()
        {
            CurrentToken?.Expire();
        }

        internal string ConnectionClientId { get; set; }

        public string ClientId => ConnectionClientId 
            ?? CurrentToken?.ClientId 
            ?? CurrentTokenParams?.ClientId 
            ?? Options.GetClientId();

        bool HasTokenId => Options.Token.IsNotEmpty();
        public bool TokenRenewable => TokenCreatedExternally || (HasApiKey && HasTokenId == false);
        bool TokenCreatedExternally => Options.AuthUrl.IsNotEmpty() || Options.AuthCallback != null;
        bool HasApiKey => Options.Key.IsNotEmpty();

        internal void Initialise()
        {
            SetAuthMethod();

            CurrentAuthOptions = Options;
            CurrentTokenParams = Options.DefaultTokenParams;
            if(CurrentTokenParams != null)
                CurrentTokenParams.ClientId = Options.GetClientId(); //Ensure the correct ClientId is set in AblyAuth

            if (AuthMethod == AuthMethod.Basic)
            {
                LogCurrentAuthenticationMethod();
                return;
            }

            Logger.Debug("Using token authentication.");
            if (Options.TokenDetails != null)
            {
                CurrentToken = Options.TokenDetails;
            }
            else if (Options.Token.IsNotEmpty())
            {
                CurrentToken = new TokenDetails(Options.Token, Options.NowFunc);
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
            }
            else
            {
                var currentValidToken = await GetCurrentValidTokenAndRenewIfNecessaryAsync();
                if (currentValidToken == null)
                {
                    throw new AblyException("Invalid token credentials: " + CurrentToken, 40100, HttpStatusCode.Unauthorized);
                }
                
                request.Headers["Authorization"] = "Bearer " + CurrentToken.Token.ToBase64();
            }
        }

        public async Task<TokenDetails> GetCurrentValidTokenAndRenewIfNecessaryAsync()
        {
            if(AuthMethod == AuthMethod.Basic)
                throw new AblyException("AuthMethod is set to Auth so there is no current valid token.");

            if (CurrentToken.IsValidToken())
                return CurrentToken;

            if (TokenRenewable)
            {
                var token = await AuthoriseAsync();
                if (token.IsValidToken())
                {
                    CurrentToken = token;       
                    return token;
                }
            }

            return null;
        }

        /// <summary>
        /// Makes a token request. This will make a token request now, even if the library already
        /// has a valid token. It would typically be used to issue tokens for use by other clients.
        /// </summary>
        /// <param name="tokenParams">The <see cref="TokenRequest"/> data used for the token</param>
        /// <param name="options">Extra <see cref="AuthOptions"/> used for creating a token </param>
        /// <returns>A valid ably token</returns>
        /// <exception cref="AblyException"></exception>
        public virtual async Task<TokenDetails> RequestTokenAsync(TokenParams tokenParams = null, AuthOptions options = null)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;
            string keyId = "", keyValue = "";
            if (mergedOptions.Key.IsNotEmpty())
            {
                var key = mergedOptions.ParseKey();
                keyId = key.KeyName;
                keyValue = key.KeySecret;
            }

            var @params = MergeTokenParamsWithDefaults(tokenParams);

            if (mergedOptions.QueryTime.GetValueOrDefault(false))
                @params.Timestamp = await _rest.TimeAsync();

            EnsureSecureConnection();

            var request = _rest.CreatePostRequest($"/keys/{keyId}/requestToken");
            request.SkipAuthentication = true;
            TokenRequest postData = null;
            if (mergedOptions.AuthCallback != null)
            {
                var callbackResult = await mergedOptions.AuthCallback(@params);

                if(callbackResult == null)
                    throw new AblyException("AuthCallback returned null");

                if (callbackResult is TokenDetails)
                    return callbackResult as TokenDetails;

                if (callbackResult is TokenRequest || callbackResult is string)
                {
                    postData = GetTokenRequest(callbackResult);

                    request.Url = $"/keys/{postData.KeyName}/requestToken";
                }
                
                else
                {
                    throw new AblyException($"AuthCallback returned an unsupported type ({callbackResult.GetType()}. Expected either TokenDetails or TokenRequest");
                }
            }
            else if (mergedOptions.AuthUrl.IsNotEmpty())
            {
                var response = await CallAuthUrl(mergedOptions, @params);

                if (response.Type == ResponseType.Text) //Return token string
                    return new TokenDetails(response.TextResponse, Now);

                var signedData = response.TextResponse;
                var jData = JObject.Parse(signedData);

                if (TokenDetails.IsToken(jData))
                    return JsonHelper.DeserializeObject<TokenDetails>(jData);

                postData = JsonHelper.Deserialize<TokenRequest>(signedData);

                request.Url = $"/keys/{postData.KeyName}/requestToken";
            }
            else
            {
                if (keyId.IsEmpty() || keyValue.IsEmpty())
                {
                    throw new AblyException("TokenAuth is on but there is no way to generate one");
                }

                postData = new TokenRequest(Now).Populate(@params, keyId, keyValue);
            }

            request.PostData = postData;

            TokenDetails result = await _rest.ExecuteRequest<TokenDetails>(request);

            if (result == null)
                throw new AblyException(new ErrorInfo("Invalid token response returned", 500));

            return result;
        }

        private static TokenRequest GetTokenRequest(object callbackResult)
        {
            if(callbackResult is TokenRequest)
                return callbackResult as TokenRequest;
            
            try
            {
                var result = JsonHelper.Deserialize<TokenRequest>((string)callbackResult);
                if(result == null)
                    throw new AblyException(new ErrorInfo($"AuthCallback returned a string which can't be converted to TokenRequest. ({callbackResult})."));

                return result;
            }
            catch (Exception e)
            {
                throw new AblyException(new ErrorInfo($"AuthCallback returned a string which can't be converted to TokenRequest. ({callbackResult})."), e);
            }
        }

        private TokenParams MergeTokenParamsWithDefaults(TokenParams tokenParams)
        {
            TokenParams @params = tokenParams?.Merge(CurrentTokenParams);

            if (@params == null)
            {
                @params = CurrentTokenParams ?? TokenParams.WithDefaultsApplied();
                @params.ClientId = ClientId; //Ensure the correct clientId is supplied
            }
            
            return @params;
        }

        private async Task<AblyResponse> CallAuthUrl(AuthOptions mergedOptions, TokenParams @params)
        {
            var url = mergedOptions.AuthUrl;
            var protocol = Options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
            var authRequest = new AblyRequest(url.ToString(), mergedOptions.AuthMethod, protocol);

            if (mergedOptions.AuthMethod == HttpMethod.Get)
            {
                authRequest.AddQueryParameters(@params.ToRequestParams(mergedOptions.AuthParams));
            }
            else
            {
                authRequest.PostParameters = @params.ToRequestParams(mergedOptions.AuthParams);
            }

            authRequest.Headers = authRequest.Headers.Merge(mergedOptions.AuthHeaders);
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
        /// <returns>Returns a valid token</returns>
        /// <exception cref="AblyException">Throws an ably exception representing the server response</exception>
        public async Task<TokenDetails> AuthoriseAsync(TokenParams tokenParams = null, AuthOptions options = null)
        {
            var authOptions = options ?? new AuthOptions();
            bool force = authOptions.Force; //this is needed because I share the object and reset Force later on.

            authOptions.Merge(CurrentAuthOptions);
            SetCurrentAuthOptions(options);

            var authTokenParams = MergeTokenParamsWithDefaults(tokenParams);
            SetCurrentTokenParams(authTokenParams);
                
            if (force)
            {
                CurrentToken = await RequestTokenAsync(authTokenParams, options);
            }
            else if (CurrentToken != null)
            {
                if (Now().AddSeconds(Defaults.TokenExpireBufferInSeconds) >= CurrentToken.Expires)
                {
                    CurrentToken = await RequestTokenAsync(authTokenParams, options);
                }
            }
            else
            {
                CurrentToken = await RequestTokenAsync(authTokenParams, options);
            }

            AuthMethod = AuthMethod.Token;
            return CurrentToken;
        }

        private void SetCurrentTokenParams(TokenParams authTokenParams)
        {
            CurrentTokenParams = authTokenParams.Clone();
            CurrentTokenParams.Timestamp = null;
        }

        private void SetCurrentAuthOptions(AuthOptions options)
        {
            if (options != null)
            {
                CurrentAuthOptions = options;
                CurrentAuthOptions.Force = false;
            }
        }

        /// <summary>
        /// Create a signed token request based on known credentials
        /// and the given token params. This would typically be used if creating
        /// signed requests for submission by another client.
        /// </summary>
        /// <param name="tokenParams"><see cref="TokenParams"/>. If null a token request is generated from options passed when the client was created.</param>
        /// <param name="authOptions"><see cref="AuthOptions"/>. If null the default AuthOptions are used.</param>
        /// <returns></returns>
        public async Task<string> CreateTokenRequestAsync(TokenParams tokenParams, AuthOptions authOptions)
        {
            var mergedOptions = authOptions != null ? authOptions.Merge(Options) : Options;

            if (string.IsNullOrEmpty(mergedOptions.Key))
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);

            var @params = MergeTokenParamsWithDefaults(tokenParams);

            if (mergedOptions.QueryTime.GetValueOrDefault(false))
                @params.Timestamp = await _rest.TimeAsync();

            ApiKey key = mergedOptions.ParseKey();
            var request = new TokenRequest(Now).Populate(@params, key.KeyName, key.KeySecret);
            return JsonHelper.Serialize(request);
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
            if (Logger.IsDebug)
            {
                TokenAuthMethod method = GetTokenAuthMethod();
                string authMethodDescription;
                switch (method)
                {
                    case TokenAuthMethod.None:
                        authMethodDescription = "None, no authentication parameters";
                        break;
                    case TokenAuthMethod.Callback:
                        authMethodDescription = "Token auth with callback";
                        break;
                    case TokenAuthMethod.Url:
                        authMethodDescription = "Token auth with URL";
                        break;
                    case TokenAuthMethod.Signing:
                        authMethodDescription = "Token auth with client-side signing";
                        break;
                    case TokenAuthMethod.JustToken:
                        authMethodDescription = "Token auth with supplied token only";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Logger.Debug("Authentication method: {0}", authMethodDescription);
            }
        }

        public Result ValidateClientIds(IEnumerable<IMessage> messages)
        {
            var libClientId = ClientId;
            if (libClientId.IsEmpty() || libClientId == "*")
                return Result.Ok();

            foreach (var message in messages)
            {
                if (message.ClientId.IsNotEmpty() && message.ClientId != libClientId)
                {
                    var errorMessage = "";
                    if (message is Message)
                    {
                        errorMessage =
                            $"{message.GetType().Name} with name '{(message as Message).Name}' has incompatible clientId {message.ClientId} as the current client is configured with {libClientId}";
                    }
                    else
                    {
                        errorMessage =
                            $"{message.GetType().Name} has incompatible clientId '{message.ClientId}' as the current client is configured with '{libClientId}'";
                    }

                    return Result.Fail(new ErrorInfo(errorMessage, 40012, (HttpStatusCode)400));
                }
            }

            return Result.Ok();
        }


        public TokenDetails RequestToken(TokenParams tokenParams = null,
            AuthOptions options = null)
        {
            return AsyncHelper.RunSync(() => RequestTokenAsync(tokenParams, options));
        }

        public TokenDetails Authorise(TokenParams tokenParams = null,
            AuthOptions options = null)
        {
            return AsyncHelper.RunSync(() => AuthoriseAsync(tokenParams, options));
        }

        public string CreateTokenRequest(TokenParams tokenParams = null,
            AuthOptions authOptions = null)
        {
            return AsyncHelper.RunSync(() => CreateTokenRequestAsync(tokenParams, authOptions));
        }
    }
}