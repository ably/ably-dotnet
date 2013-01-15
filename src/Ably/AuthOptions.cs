using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class AuthOptions
    {
        public Func<RequestTokenParams, string> AuthCallback;
        public String AuthUrl { get; set; }
        public String Key { get; set; }
        public String KeyId { get; set; }
        public String KeyValue { get; set; }
        public String AuthToken { get; set; }
        public IList<string> AuthHeaders { get; set; }
        public IList<string> AuthParams { get; set; }
        public bool QueryTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the AuthOptions class.
        /// </summary>
        public AuthOptions()
        {
            AuthHeaders = new List<string>();
            AuthParams = new List<string>();
        }

        public AuthOptions Merge(AuthOptions defaults)
        {
            if (AuthCallback == null) AuthCallback = defaults.AuthCallback;
            if (AuthUrl == null) AuthUrl = defaults.AuthUrl;
            if (KeyId == null) KeyId = defaults.KeyId;
            if (KeyValue == null) KeyValue = defaults.KeyValue;
            if (AuthHeaders.Count == 0) ((List<string>)AuthHeaders).AddRange(defaults.AuthHeaders);
            if (AuthParams.Count == 0) ((List<string>)AuthParams).AddRange(defaults.AuthParams);
            QueryTime = QueryTime || defaults.QueryTime;
            return this;
        }
    }
}
