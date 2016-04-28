﻿using System;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Http;
using IO.Ably.Auth;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace IO.Ably
{
    internal class AblyAuth : IAuthCommands
    {
        internal AblyAuth(ClientOptions options, AblyRest rest)
        {
            Options = options;
            _rest = rest;

            Initialise();
        }

        internal AuthMethod AuthMethod;
        internal ClientOptions Options { get; }
        internal TokenParams CurrentTokenParams { get; set; }
        internal AuthOptions CurrentAuthOptions { get; set; }
        private readonly AblyRest _rest;

        public TokenDetails CurrentToken { get; set; }

        public string GetClientId()
        {
            return CurrentToken?.ClientId ?? CurrentTokenParams?.ClientId ?? Options.GetClientId();
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

            CurrentAuthOptions = Options;
            CurrentTokenParams = Options.DefaultTokenParams;
            if(CurrentTokenParams != null)
                CurrentTokenParams.ClientId = Options.GetClientId(); //Ensure the correct ClientId is set in AblyAuth

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
                    CurrentToken = await Authorise();
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
        public virtual async Task<TokenDetails> RequestToken(TokenParams tokenParams = null, AuthOptions options = null)
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
                @params.Timestamp = await _rest.Time();

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
                var response = await CallAuthUrl(mergedOptions, @params);

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

            request.PostData = postData;

            TokenDetails result = await _rest.ExecuteRequest<TokenDetails>(request);

            if (null == result)
                throw new AblyException(new ErrorInfo("Invalid token response returned", 500));
            return result;
        }

        private TokenParams MergeTokenParamsWithDefaults(TokenParams tokenParams)
        {
            TokenParams @params = tokenParams?.Merge(CurrentTokenParams);

            if (@params == null)
            {
                @params = CurrentTokenParams ?? TokenParams.WithDefaultsApplied();
                @params.ClientId = GetClientId(); //Ensure the correct clientId is supplied
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
        public async Task<TokenDetails> Authorise(TokenParams tokenParams = null, AuthOptions options = null)
        {
            var authOptions = options ?? new AuthOptions();
            bool force = authOptions.Force; //this is needed because I share the object and reset Force later on.

            authOptions.Merge(CurrentAuthOptions);
            SetCurrentAuthOptions(options);

            var authTokenParams = MergeTokenParamsWithDefaults(tokenParams);
            SetCurrentTokenParams(authTokenParams);
                
            if (force)
            {
                CurrentToken = await RequestToken(authTokenParams, options);
            }
            else if (CurrentToken != null)
            {
                if ((Config.Now().AddSeconds(Defaults.TokenExpireBufferInSeconds)) >= CurrentToken.Expires)
                {
                    CurrentToken = await RequestToken(authTokenParams, options);
                }
            }
            else
            {
                CurrentToken = await RequestToken(authTokenParams, options);
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
        /// <param name="options"><see cref="AuthOptions"/>. If null the default AuthOptions are used.</param>
        /// <returns></returns>
        public async Task<TokenRequest> CreateTokenRequest(TokenParams tokenParams, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(Options) : Options;

            if (string.IsNullOrEmpty(mergedOptions.Key))
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);

            var @params = MergeTokenParamsWithDefaults(tokenParams);

            if (mergedOptions.QueryTime.GetValueOrDefault(false))
                @params.Timestamp = await _rest.Time();

            ApiKey key = mergedOptions.ParseKey();
            var request = new TokenRequest().Populate(@params, key.KeyName, key.KeySecret);

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