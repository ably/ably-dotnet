using System;
using System.Net;
using System.Net.Http;
using IO.Ably.Auth;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace IO.Ably
{
    public class AblyTokenAuth : IAuthCommands
    {
        internal AblyTokenAuth(ClientOptions options, Rest.IAblyRest rest)
        {
            _options = options;
            _rest = rest;
        }

        private ClientOptions _options;
        private TokenParams _lastTokenRequest;
        private Rest.IAblyRest _rest;
        // Buffer in seconds before a token is considered unusable
        private const int TokenExpireBufer = 15;

        //TODO: MG: Temporary ugliness. I want to get the tests passing before fixing this shit.
        private TokenDetails CurrentToken
        {
            get { return (_rest as AblyRest).CurrentToken; }
            set { (_rest as AblyRest).CurrentToken = value; }
        }

        /// <summary>
        /// Makes a token request. This will make a token request now, even if the library already
	    /// has a valid token. It would typically be used to issue tokens for use by other clients.
        /// </summary>
        /// <param name="requestData">The <see cref="TokenRequest"/> data used for the token</param>
        /// <param name="options">Extra <see cref="AuthOptions"/> used for creating a token </param>
        /// <returns>A valid ably token</returns>
        /// <exception cref="AblyException"></exception>
        public async Task<TokenDetails> RequestToken(TokenParams requestData, AuthOptions options)
        {
            var mergedOptions = options != null ? options.Merge(_options) : _options;
            string keyId = "", keyValue = "";
            if (mergedOptions.Key.IsNotEmpty())
            {
                var key = mergedOptions.ParseKey();
                keyId = key.KeyName;
                keyValue = key.KeySecret;
            }

            var data = requestData ?? new TokenParams()
            {
                ClientId = _options.ClientId
            };

            if (requestData == null && options == null && _lastTokenRequest != null)
            {
                data = _lastTokenRequest;
            }

            _lastTokenRequest = data;

            var request = _rest.CreatePostRequest($"/keys/{keyId}/requestToken");
            request.SkipAuthentication = true;
            TokenRequest postData = null;
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
                var protocol = _options.UseBinaryProtocol == false ? Protocol.Json : Protocol.MsgPack;
                var authRequest = new AblyRequest(url, mergedOptions.AuthMethod, protocol);
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
                if (response.Type != ResponseType.Json)
                    throw new AblyException(
                        new ErrorInfo(
                            string.Format("Content Type {0} is not supported by this client library",
                                response.ContentType), 500));

                var signedData = response.TextResponse;
                var jData = JObject.Parse(signedData);
                if (TokenDetails.IsToken(jData))
                    return jData.ToObject<TokenDetails>();

                postData = JsonConvert.DeserializeObject<TokenRequest>(signedData);

                request.Url = $"/keys/{postData.KeyName}/requestToken";
            }
            else
            {
                postData = new TokenRequest().Populate(data, keyId, keyValue);
            }

            if (mergedOptions.QueryTime)
                postData.Timestamp = (await _rest.Time()).ToUnixTimeInMilliseconds().ToString();

            request.PostData = postData;

            TokenDetails result = await _rest.ExecuteRequest<TokenDetails>(request);

            if (null == result)
                throw new AblyException(new ErrorInfo("Invalid token response returned", 500));
            return result;
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
                if (CurrentToken.Expires > (Config.Now().AddSeconds(TokenExpireBufer)))
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
            var mergedOptions = options != null ? options.Merge(_options) : _options;

            if (string.IsNullOrEmpty(mergedOptions.Key))
                throw new AblyException("No key specified", 40101, HttpStatusCode.Unauthorized);

            var data = tokenParams ?? new TokenParams
            {
                ClientId = _options.ClientId
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
    }
}