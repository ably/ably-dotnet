using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Ably.Auth;

namespace Ably
{
    public class AuthOptions
    {
        public Func<TokenRequest, Token> AuthCallback;
        public string AuthUrl { get; set; }
        /// <summary>
        /// Used in conjunction with AuthUrl. Default is GET.
        /// </summary>
        public HttpMethod AuthMethod { get; set; }
        public string Key { get; set; }
        public string KeyId { get; set; }
        public string KeyValue { get; set; }
        public string AuthToken { get; set; }
        public Dictionary<string, string> AuthHeaders { get; set; }
        public Dictionary<string, string> AuthParams { get; set; }
        public bool QueryTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the AuthOptions class.
        /// </summary>
        public AuthOptions()
        {
            AuthHeaders = new Dictionary<string,string>();
            AuthParams = new Dictionary<string, string>();
            AuthMethod = HttpMethod.Get;
        }

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
