using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public class TokenRequest
    {
        public String Id { get; set;}
        public TimeSpan? Ttl { get; set; }
        public Capability Capability { get; set; }
        public String ClientId { get; set; }

        internal TokenRequestPostData GetPostData(string keyValue)
        {
            var data = new TokenRequestPostData();
            data.id = Id;
            data.capability = Capability.ToString();
            data.client_id = ClientId;
            DateTime now = Config.Now();
            if (Ttl.HasValue)
                data.ttl = now.Add(Ttl.Value).ToUnixTime().ToString();
            else
                data.ttl = now.AddHours(1).ToUnixTime().ToString();
            data.timestamp = now.ToUnixTime().ToString();
            data.CalculateMac(keyValue);
            
            return data;
        }
    }

    internal class TokenRequestPostData
    {
        public TokenRequestPostData()
        {
            nonce = Guid.NewGuid().ToString("N").ToLower();
        }

        public string id { get; set; }
        public string ttl { get; set; }
        public string capability { get; set; }
        public string client_id { get; set; }
        public string timestamp { get; set; }
        public string nonce { get; set; }
        public string mac { get; set; }

        public void CalculateMac(string key)
        {
            var values = new[] 
            { 
                id, 
                ttl,
                capability, 
                client_id, 
                timestamp,
                nonce
            };

            var signText = string.Join("\n", values) + "\n";

            mac = signText.ComputeHMacSha256(key);
        }

        public override bool Equals(object obj)
        {
            var other = obj as TokenRequestPostData;
            if(other == null)
                return false;
            return id == other.id
                && ttl == other.ttl
                && capability == other.capability
                && client_id == other.client_id
                && timestamp == other.timestamp
                && nonce == other.nonce
                && mac == other.mac;
        }
    }
}
