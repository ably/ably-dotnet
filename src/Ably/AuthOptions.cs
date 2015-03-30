using System;
using System.Collections.Generic;

namespace Ably
{
    /// <summary>
    /// An AuthOptions object contains credentials and related options in support of interacting with the Ably 
    /// service. AuthOptions are a subset of the Ably library Options. Various methods on the Auth object take 
    /// an AuthOptions argumentone or more AuthOptions options to enable the authentication of the 
    /// requesting client to the service.
    /// 
    /// Options specified in this way will supplement or override the corresponding options given when the 
    /// library was instanced.
    /// </summary>
    public class AuthOptions
    {
        public AuthOptions()
        {
            AuthHeaders = new Dictionary<string, string>();
            AuthParams = new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates AuthOptions based on the key string obtained from the application dashboard.
        /// </summary>
        /// <param name="key">key: the full key string as obtained from the dashboard</param>
        /// <exception cref="AblyException" />
        public AuthOptions(string key)
        {
            this.Key = key;
            string[] keyParts = key.Split(':');
            if (keyParts.Length != 2)
            {
                string msg = "invalid key parameter";
                throw new AblyException(msg, 40101, System.Net.HttpStatusCode.Unauthorized);
            }
            KeyId = keyParts[0];
            KeyValue = keyParts[1];
        }

        /// <summary>
        /// A function to call when a new token is required. The role of the callback 
        /// is to generate a signed token request which may then be submitted by the
        /// library to the requestToken API.
        /// </summary>
        public Func<TokenRequest, string> AuthCallback;

        /// <summary>
        /// A URL to query to obtain a signed token request. This enables a client to obtain token requests 
        /// from another entity, so tokens can be renewed without the client requiring access to keys.
        /// </summary>
        public string AuthUrl { get;set; }

        /// <summary>
        /// A keyId. This is used in instances where a full key or token are not provided at initialisation.
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// The full key string, as obtained from the application dashboard. Use this 
        /// option if you wish to use Basic authentication, or wish to be able to 
        /// issue tokens without needing to defer to a separate entity to sign token 
        /// requests.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The secret keyValue for an Ably key associated with this client.
        /// </summary>
        public string KeyValue { get; set; }

        /// <summary>
        /// An authentication token issued for this application against a specific key and TokenParams
        /// </summary>
        public string AuthToken { get; set; }

        /// <summary>
        /// Headers to be included in any request made by the library to the authURL.
        /// </summary>
        public Dictionary<string, string> AuthHeaders { get; set; }

        /// <summary>
        /// Query params to be included in any request made by the library to the authURL.
        /// </summary>
        public Dictionary<string, string> AuthParams { get; set; }

        /// <summary>
        /// This may be set in instances that the library is to sign token requests based on a given key. If 
        /// true, the library will query the Ably system for the current time instead of relying on a 
        /// locally-available time of day.
        /// </summary>
        public bool QueryTime { get; set; }

        public AuthOptions Merge(AuthOptions defaults)
        {
            if (AuthCallback == null) AuthCallback = defaults.AuthCallback;
            if (AuthUrl == null) AuthUrl = defaults.AuthUrl;
            if (KeyId == null) KeyId = defaults.KeyId;
            if (KeyValue == null) KeyValue = defaults.KeyValue;
            if (AuthHeaders.Count == 0) AuthHeaders = defaults.AuthHeaders;
            if (AuthParams.Count == 0) AuthParams = defaults.AuthParams;
            QueryTime = QueryTime || defaults.QueryTime;
            return this;
        }
    }
}
