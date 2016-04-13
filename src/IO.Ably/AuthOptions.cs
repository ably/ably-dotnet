using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Auth;

namespace IO.Ably
{

    /// <summary>
    /// Authentication options
    /// </summary>
    public class AuthOptions
    {
        /// <summary>
        /// Callback used when requesting a new token. A <see cref="TokenRequest"/> is passed and it needs to return <see cref="TokenDetails"/>
        /// </summary>
        public Func<TokenParams, Task<TokenDetails>> AuthCallback;

        /// <summary>
        /// A URL to query to obtain either a signed token request (<see cref="TokenRequest"/>) or a valid <see cref="TokenDetails"/>
        /// This enables a client to obtain token requests from
        /// another entity, so tokens can be renewed without the
        /// client requiring access to keys.
        ///</summary>
        public Uri AuthUrl { get; set; }

        /// <summary>
        /// Used in conjunction with AuthUrl. Default is GET.
        /// </summary>
        public HttpMethod AuthMethod { get; set; }

        public string Key { get; set; }
        public string Token { get; set; }
        public TokenDetails TokenDetails { get; set; }

        public Dictionary<string, string> AuthHeaders { get; set; }
        public Dictionary<string, string> AuthParams { get; set; }
        public bool QueryTime { get; set; }
        public bool? UseTokenAuth { get; set; }

        /// <summary>
        /// Initializes a new instance of the AuthOptions class.
        /// </summary>
        public AuthOptions()
        {
            AuthHeaders = new Dictionary<string,string>();
            AuthParams = new Dictionary<string, string>();
            AuthMethod = HttpMethod.Get;
        }

        /// <summary>
        /// Initialized a new instance of AuthOptions by populating the KeyId and KeyValue properties from the full Key
        /// </summary>
        /// <param name="key">Full ably key string</param>
        public AuthOptions(string key)
            : this()
        {
            var apiKey = ApiKey.Parse(key);
            Key = apiKey.ToString();
        }

        public AuthOptions Merge(AuthOptions defaults)
        {
            if (AuthCallback == null) AuthCallback = defaults.AuthCallback;
            if (AuthUrl == null) AuthUrl = defaults.AuthUrl;
            if (AuthHeaders.Count == 0) AuthHeaders = defaults.AuthHeaders;
            if (AuthParams.Count == 0) AuthParams = defaults.AuthParams;
            if (Key.IsEmpty()) Key = defaults.Key;
            if (UseTokenAuth.HasValue == false) UseTokenAuth = defaults.UseTokenAuth;
            QueryTime = QueryTime || defaults.QueryTime;
            return this;
        }

        internal ApiKey ParseKey()
        {
            return ApiKey.Parse(Key);
        }
    }
}
