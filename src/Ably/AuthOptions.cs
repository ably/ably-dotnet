using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class AuthOptions
    {
        public Func<TokenRequest, string> AuthCallback;
        public String AuthUrl { get; set; }
        public String Key { get; set; }
        public String KeyId { get; set; }
        public String KeyValue { get; set; }
        public String AuthToken { get; set; }
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
