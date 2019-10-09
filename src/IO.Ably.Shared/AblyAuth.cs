using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably
{
    internal class AblyAuth : IAblyAuth
    {
        public Func<TokenDetails, bool, Task> OnAuthUpdated = (token, wait) => Task.CompletedTask; // By default nothing should happen

        private readonly AblyRest _rest;

        internal AblyAuth(ClientOptions options, AblyRest rest)
        {
            Now = options.NowFunc;
            Options = options;
            _rest = rest;
            Logger = options.Logger;
            ServerTime = () => _rest.TimeAsync();
            ServerTimeOffset = () => null;
            Initialise();
        }

        protected Func<Task<DateTimeOffset>> ServerTime { get; set; }

        protected Func<DateTimeOffset?> ServerTimeOffset { get; private set; }

        internal Func<DateTimeOffset> Now { get; set; }

        internal ILogger Logger { get; private set; }

        public AuthMethod AuthMethod { get; private set; }

        internal ClientOptions Options { get; }

        internal TokenParams CurrentTokenParams { get; set; }

        internal AuthOptions CurrentAuthOptions { get; set; }

        public TokenDetails CurrentToken { get; set; }

        internal string ConnectionClientId { get; set; }

        public string ClientId => ConnectionClientId
            ?? CurrentToken?.ClientId
            ?? CurrentTokenParams?.ClientId
            ?? Options.GetClientId();

        private bool HasTokenId => Options.Token.IsNotEmpty();

        public bool TokenRenewable => TokenCreatedExternally || (HasApiKey && HasTokenId == false);

        private bool TokenCreatedExternally => Options.AuthUrl.IsNotEmpty() || Options.AuthCallback != null;

        private bool HasApiKey => Options.Key.IsNotEmpty();

        public void ExpireCurrentToken()
        {
            CurrentToken?.Expire();
        }

        internal void Initialise()
        {
            SetAuthMethod();

            CurrentAuthOptions = Options;
            CurrentTokenParams = Options.DefaultTokenParams;
            if (CurrentTokenParams != null)
            {
                CurrentTokenParams.ClientId = Options.GetClientId(); // Ensure the correct ClientId is set in AblyAuth
            }

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

        private async Task SetServerTimeOffset()
        {
            TimeSpan diff = Now() - await ServerTime();
            ServerTimeOffset = () => Now() - diff;
        }

        private void SetAuthMethod()
        {
            if (Options.UseTokenAuth.HasValue)
            {
                // ASK: Should I throw an error if a particular auth is not possible
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
            {
                request.Headers.Remove("Authorization");
            }

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
            if (AuthMethod == AuthMethod.Basic)
            {
                throw new AblyException("AuthMethod is set to Auth so there is no current valid token.");
            }

            if (CurrentToken.IsValidToken())
            {
                return CurrentToken;
            }

            return await RenewToken();
        }

        /// <summary>
        /// Renews the current token and calls OnAuthUpdated without blocking until the connection is reestablished.
        /// </summary>
        /// <returns>new token if successful.</returns>
        /// <exception cref="AblyException">Throws an exception if the new token is not valid.</exception>
        internal async Task<TokenDetails> RenewToken()
        {
            if (TokenRenewable)
            {
                var token = await RequestTokenAsync();

                await OnAuthUpdated(token, false);

                if (token.IsValidToken())
                {
                    CurrentToken = token;
                    return token;
                }

                if (token != null && token.IsExpired())
                {
                    throw new AblyException("Token has expired: " + CurrentToken, 40142, HttpStatusCode.Unauthorized);
                }
            }
            else
            {
                Logger.Debug("Auth.RenewToken called but token was not Renewable");
            }

            return null;
        }

        /// <summary>
        /// Makes a token request. This will make a token request now, even if the library already
        /// has a valid token. It would typically be used to issue tokens for use by other clients.
        /// </summary>
        /// <param name="tokenParams">The <see cref="TokenRequest"/> data used for the token.</param>
        /// <param name="authOptions">Extra <see cref="AuthOptions"/> used for creating a token.</param>
        /// <returns>A valid ably token.</returns>
        /// <exception cref="AblyException">something went wrong.</exception>
        public virtual async Task<TokenDetails> RequestTokenAsync(TokenParams tokenParams = null, AuthOptions authOptions = null)
        {
            EnsureSecureConnection();

            // (RSA8e)
            authOptions = authOptions ?? CurrentAuthOptions ?? Options ?? new AuthOptions();
            tokenParams = tokenParams ?? CurrentTokenParams ?? TokenParams.WithDefaultsApplied();

            string keyId = string.Empty, keyValue = string.Empty;
            if (authOptions.Key.IsNotEmpty())
            {
                var key = authOptions.ParseKey();
                keyId = key.KeyName;
                keyValue = key.KeySecret;
            }

            if (tokenParams.ClientId.IsEmpty())
            {
                tokenParams.ClientId = ClientId;
            }

            await SetTokenParamsTimestamp(authOptions, tokenParams);

            var request = _rest.CreatePostRequest($"/keys/{keyId}/requestToken");
            request.SkipAuthentication = true;
            TokenRequest postData = null;
            if (authOptions.AuthCallback != null)
            {
                bool shouldCatch = true;
                try
                {
                    var callbackResult = await authOptions.AuthCallback(tokenParams);

                    if (callbackResult == null)
                    {
                        throw new AblyException("AuthCallback returned null", 80019);
                    }

                    if (callbackResult is TokenDetails)
                    {
                        return callbackResult as TokenDetails;
                    }

                    if (callbackResult is TokenRequest || callbackResult is string)
                    {
                        postData = GetTokenRequest(callbackResult);
                        request.Url = $"/keys/{postData.KeyName}/requestToken";
                    }
                    else
                    {
                        shouldCatch = false;
                        throw new AblyException($"AuthCallback returned an unsupported type ({callbackResult.GetType()}. Expected either TokenDetails or TokenRequest", 80019, HttpStatusCode.BadRequest);
                    }
                }
                catch (Exception ex) when (shouldCatch)
                {
                    var statusCode = HttpStatusCode.Unauthorized;
                    if (ex is AblyException aex)
                    {
                        statusCode = aex.ErrorInfo.StatusCode == HttpStatusCode.Forbidden
                            ? HttpStatusCode.Forbidden
                            : HttpStatusCode.Unauthorized;
                    }

                    throw new AblyException(
                        new ErrorInfo(
                        "Error calling AuthCallback, token request failed. See inner exception for details.",
                        80019,
                        statusCode), ex);
                }
            }
            else if (authOptions.AuthUrl.IsNotEmpty())
            {
                var responseText = string.Empty;
                try
                {
                    var response = await CallAuthUrl(authOptions, tokenParams);

                    if (response.Type == ResponseType.Text || response.Type == ResponseType.Jwt)
                    {
                        // RSC8c:
                        // The token retrieved is assumed by the library to be a token string
                        // if the response has Content-Type "text/plain" or "application/jwt"
                        return new TokenDetails(response.TextResponse, Now);
                    }

                    responseText = response.TextResponse;
                    var jData = JObject.Parse(responseText);

                    if (TokenDetails.IsToken(jData))
                    {
                        return JsonHelper.DeserializeObject<TokenDetails>(jData);
                    }

                    postData = JsonHelper.Deserialize<TokenRequest>(responseText);

                    request.Url = $"/keys/{postData.KeyName}/requestToken";
                }
                catch (AblyException ex)
                {
                    var statusCode = ex.ErrorInfo.StatusCode == HttpStatusCode.Forbidden
                                        ? ex.ErrorInfo.StatusCode
                                        : HttpStatusCode.Unauthorized;
                    throw new AblyException(
                        new ErrorInfo(
                            "Error calling Auth URL, token request failed. See the InnerException property for details of the underlying exception.",
                            80019,
                            statusCode,
                            ex),
                        ex);
                }
                catch (Exception ex)
                {
                    string reason =
                        "Error handling Auth URL, token request failed. See the InnerException property for details of the underlying exception.";

                    if (ex is JsonReaderException)
                    {
                        reason =
                            $"Error parsing JSON response '{responseText}' from Auth URL.See the InnerException property for details of the underlying exception.";
                    }

                    throw new AblyException(
                        new ErrorInfo(
                            reason,
                            80019,
                            HttpStatusCode.InternalServerError,
                            ex),
                        ex);
                }
            }
            else
            {
                if (keyId.IsEmpty() || keyValue.IsEmpty())
                {
                    throw new AblyException("TokenAuth is on but there is no way to generate one", 80019);
                }

                postData = new TokenRequest(Now).Populate(tokenParams, keyId, keyValue);
            }

            request.PostData = postData;

            TokenDetails result = await _rest.ExecuteRequest<TokenDetails>(request);

            if (result == null)
            {
                throw new AblyException("Invalid token response returned", 80019);
            }

            return result;
        }

        private async Task SetTokenParamsTimestamp(AuthOptions authOptions, TokenParams tokenParams)
        {
            if (authOptions.QueryTime.GetValueOrDefault(false)
                && !ServerTimeOffset().HasValue)
            {
                await SetServerTimeOffset();
            }

            if (!tokenParams.Timestamp.HasValue)
            {
                tokenParams.Timestamp = ServerTimeOffset();
            }
        }

#pragma warning disable SA1204 // Static elements should appear before instance elements
        private static TokenRequest GetTokenRequest(object callbackResult)
#pragma warning restore SA1204 // Static elements should appear before instance elements
        {
            if (callbackResult is TokenRequest)
            {
                return callbackResult as TokenRequest;
            }

            try
            {
                var result = JsonHelper.Deserialize<TokenRequest>((string)callbackResult);
                if (result == null)
                {
                    throw new AblyException(new ErrorInfo($"AuthCallback returned a string which can't be converted to TokenRequest. ({callbackResult})."));
                }

                return result;
            }
            catch (Exception e)
            {
                throw new AblyException(new ErrorInfo($"AuthCallback returned a string which can't be converted to TokenRequest. ({callbackResult})."), e);
            }
        }

        private async Task<AblyResponse> CallAuthUrl(AuthOptions mergedOptions, TokenParams @params)
        {
            var url = mergedOptions.AuthUrl;
#if MSGPACK
            var protocol = Options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
#else
            var protocol = Defaults.Protocol;
#endif
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
            {
                throw new AblyException(
                    new ErrorInfo($"Content Type {response.ContentType} is not supported by this client library", 500));
            }

            return response;
        }

        private void EnsureSecureConnection()
        {
            if (AuthMethod == AuthMethod.Basic && Options.Tls == false)
            {
                throw new InsecureRequestException();
            }
        }

        /// <summary>
        /// Ensure valid auth credentials are present. This may rely in an already-known
        /// and valid token, and will obtain a new token if necessary or explicitly
        /// requested.
        /// Authorisation will use the parameters supplied on construction except
        /// where overridden with the options supplied in the call.
        /// </summary>
        /// <param name="tokenParams"><see cref="TokenParams"/> custom parameter. Pass null and default token request options will be generated used the options passed when creating the client.</param>
        /// <param name="authOptions"><see cref="AuthOptions"/> custom options.</param>
        /// <returns>Returns a valid token.</returns>
        /// <exception cref="AblyException">Throws an ably exception representing the server response.</exception>
        public async Task<TokenDetails> AuthorizeAsync(TokenParams tokenParams = null, AuthOptions authOptions = null)
        {
            // RSA10j - TokenParams and AuthOptions supersede any previously client library configured TokenParams and AuthOptions
            authOptions = authOptions ?? CurrentAuthOptions ?? Options;
            SetCurrentAuthOptions(authOptions);

            tokenParams = tokenParams ?? CurrentTokenParams ?? TokenParams.WithDefaultsApplied();
            SetCurrentTokenParams(tokenParams);

            CurrentToken = await RequestTokenAsync(tokenParams, authOptions);
            AuthMethod = AuthMethod.Token;

            // RTC8a3 - wait for reconnect if it's the Realtime client
            await OnAuthUpdated(CurrentToken, true);

            return CurrentToken;
        }

        [Obsolete("This method will be removed in the future, please replace with a call to AuthorizeAsync")]
        public async Task<TokenDetails> AuthoriseAsync(TokenParams tokenParams = null, AuthOptions options = null)
        {
            Logger.Warning("AuthoriseAsync is deprecated and will be removed in the future, please replace with a call to AuthorizeAsync");
            return await AuthorizeAsync(tokenParams, options);
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
            }
        }

        /// <summary>
        /// Create a signed token request based on known credentials
        /// and the given token params. This would typically be used if creating
        /// signed requests for submission by another client.
        /// </summary>
        /// <param name="tokenParams"><see cref="TokenParams"/>. If null a token request is generated from options passed when the client was created.</param>
        /// <param name="authOptions"><see cref="AuthOptions"/>. If null the default AuthOptions are used.</param>
        /// <returns>signed token request.</returns>
        public async Task<string> CreateTokenRequestAsync(TokenParams tokenParams, AuthOptions authOptions)
        {
            authOptions = authOptions ?? CurrentAuthOptions ?? Options;
            tokenParams = tokenParams ?? CurrentTokenParams ?? TokenParams.WithDefaultsApplied();

            if (string.IsNullOrEmpty(authOptions.Key))
            {
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);
            }

            await SetTokenParamsTimestamp(authOptions, tokenParams);

            if (authOptions.QueryTime.GetValueOrDefault(false))
            {
                tokenParams.Timestamp = await _rest.TimeAsync();
            }

            var apiKey = authOptions.ParseKey();
            var request = new TokenRequest(Now).Populate(tokenParams, apiKey.KeyName, apiKey.KeySecret);
            return JsonHelper.Serialize(request);
        }

        internal TokenAuthMethod GetTokenAuthMethod()
        {
            if (Options.AuthCallback != null)
            {
                return TokenAuthMethod.Callback;
            }

            if (Options.AuthUrl.IsNotEmpty())
            {
                return TokenAuthMethod.Url;
            }

            if (Options.Key.IsNotEmpty())
            {
                return TokenAuthMethod.Signing;
            }

            if (Options.Token.IsNotEmpty())
            {
                return TokenAuthMethod.JustToken;
            }

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
            {
                return Result.Ok();
            }

            foreach (var message in messages)
            {
                if (message.ClientId.IsNotEmpty() && message.ClientId != libClientId)
                {
                    var errorMessage = string.Empty;
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

        public TokenDetails RequestToken(TokenParams tokenParams = null, AuthOptions options = null)
        {
            return AsyncHelper.RunSync(() => RequestTokenAsync(tokenParams, options));
        }

        public TokenDetails Authorize(TokenParams tokenParams = null, AuthOptions options = null)
        {
            return AsyncHelper.RunSync(() => AuthorizeAsync(tokenParams, options));
        }

        [Obsolete("This method will be removed in the future, please replace with a call to Authorize")]
        public TokenDetails Authorise(TokenParams tokenParams = null, AuthOptions options = null)
        {
            Logger.Warning("Authorise is deprecated and will be removed in the future, please replace with a call to Authorize.");
            return AsyncHelper.RunSync(() => AuthorizeAsync(tokenParams, options));
        }

        public string CreateTokenRequest(TokenParams tokenParams = null, AuthOptions authOptions = null)
        {
            return AsyncHelper.RunSync(() => CreateTokenRequestAsync(tokenParams, authOptions));
        }
    }
}
