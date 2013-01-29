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
            data.capability = Capability.ToJson();
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

        internal void Validate()
        {
            //if (Id.IsEmpty())
            //    new ArgumentNullException("Id", "Cannot use TokenRequest without Id").Throw();

            if (Capability == null)
                new ArgumentNullException("Capability", "Cannot user TokenRequest without Capability specified").Throw();
        }
    }
}