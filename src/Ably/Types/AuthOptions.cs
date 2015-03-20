using System;
using System.Collections.Generic;

namespace Ably.Types
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
        public AuthOptions() { }

        /// <summary>
        /// Creates AuthOptions based on the key string obtained from the application dashboard.
        /// </summary>
        /// <param name="key">key: the full key string as obtained from the dashboard</param>
        /// <exception cref="AblyException" />
        public AuthOptions(string key)
        {
            string[] keyParts = key.Split(':');
            if (keyParts.Length != 2)
            {
                string msg = "invalid key parameter";
                throw new AblyException(msg, 401, 40101);
            }
            KeyId = keyParts[0];
            KeyValue = keyParts[1];
        }

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
        //public Param[] AuthHeaders { get; set; }

        /// <summary>
        /// Query params to be included in any request made by the library to the authURL.
        /// </summary>
        //public Param[] AuthParams { get; set; }

        /// <summary>
        /// This may be set in instances that the library is to sign token requests based on a given key. If 
        /// true, the library will query the Ably system for the current time instead of relying on a 
        /// locally-available time of day.
        /// </summary>
        public bool QueryTime { get; set; }
    }
}
