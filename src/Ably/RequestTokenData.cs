using System;

namespace Ably
{
    public class TokenRequest
    {
        public string Id { get; set;}
        public TimeSpan? Ttl { get; set; }
        public Capability Capability { get; set; }
        public string ClientId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string Nonce { get; set;}

        internal TokenRequestPostData GetPostData(string keyValue)
        {
            var data = new TokenRequestPostData();
            data.id = Id;
            data.capability = Capability.ToJson();
            data.client_id = ClientId ?? "";
            DateTime now = Config.Now();
            if (Nonce.IsNotEmpty())
                data.nonce = Nonce;
            if (Ttl.HasValue)
                data.ttl = Ttl.Value.TotalSeconds.ToString();
            else
                data.ttl = TimeSpan.FromHours(1).TotalSeconds.ToString();
            if (Timestamp.HasValue)
                data.timestamp = Timestamp.Value.ToUnixTime().ToString();
            else
                data.timestamp = now.ToUnixTime().ToString();
            data.CalculateMac(keyValue);
            
            return data;
        }

        internal void Validate()
        {
            //if (Id.IsEmpty())
            //    new ArgumentNullException("Id", "Cannot use TokenRequest without Id").Throw();

            if (Capability == null)
                throw new AblyException("Cannot user TokenRequest without Capability specified");
        }
    }
}