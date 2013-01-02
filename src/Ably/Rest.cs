using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ably
{
    public class Rest
    {
        private ApiKey _key;

        public Rest()
        {
            var key = GetConnectionString();
            if (string.IsNullOrEmpty(key))
            {
                throw new ConfigurationMissingException("A connection strig with key 'Ably' doesn't exist in the application configuration");
            }
            _key = ApiKey.Parse(key);
        }

        public Rest(string apiKey)
        {
            _key = ApiKey.Parse(apiKey);
        }

        internal virtual string GetConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings["Ably"];
            if (connString == null)
            {
                return string.Empty;
            }

            return connString.ConnectionString;
        }

        internal Func<AblyRequest, AblyResponse> ExecuteRequest = ExecuteRequestInternal;

        internal Func<DateTime> Now = () => DateTime.Now;
        
        private static AblyResponse ExecuteRequestInternal(AblyRequest request)
        {
            return null;
        }

        public string RequestToken(RequestTokenOptions options)
        {
            var request = new AblyRequest(String.Format("/apps/{0}/requestToken", _key.AppId));
            request.PostParameters.Add("id", _key.KeyId);
            TimeSpan expiresInterval = options.Expires ?? TimeSpan.FromHours(1);
            string expiresUnixTime = Now().Add(expiresInterval).ToUnixTime().ToString();
            request.PostParameters.Add("expires", expiresUnixTime);
            if(string.IsNullOrWhiteSpace(options.Capability) == false )
                request.PostParameters.Add("capability", options.Capability);
            if(string.IsNullOrWhiteSpace(options.ClientId) == false )
                request.PostParameters.Add("client_id", options.ClientId);

            request.PostParameters.Add("timestamp", Now().ToUnixTime().ToString());
            request.PostParameters.Add("nonce", Guid.NewGuid().ToString("N").ToLower());
            request.PostParameters.Add("mac", CalculateMac(request.PostParameters, _key.KeyValue));
            ExecuteRequest(request);
            return "";
        }

        private string CalculateMac(Dictionary<string, string> postParameters, string key)
        {
            var values = new[] 
            { 
                postParameters.Get("id"), 
                postParameters.Get("expires"),
                postParameters.Get("capability", ""), 
                postParameters.Get("client_id", ""), 
                postParameters.Get("timestamp"),
                postParameters.Get("nonce")
            };

            var signText = string.Join("\n", values) + "\n";

            return signText.ComputeHMacSha256(key);
        }
    }
}