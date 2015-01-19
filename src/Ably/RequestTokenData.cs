using System;
using System.Globalization;

namespace Ably
{
    public class TokenRequest
    {
        public static class Defaults
        {
            public static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
            public static readonly Capability Capability = Capability.AllowAll;
        }

        public string Id { get; set;}
        public TimeSpan? Ttl { get; set; }
        public Capability Capability { get; set; }
        public string ClientId { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string Nonce { get; set;}

        internal TokenRequestPostData GetPostData(string keyValue)
        {
            var data = new TokenRequestPostData();
            data.id = Id;
            data.capability = (Capability ?? Defaults.Capability).ToJson();
            data.clientId = ClientId ?? "";
            DateTimeOffset now = Config.Now();
            if (Nonce.IsNotEmpty())
                data.nonce = Nonce;
            if (Ttl.HasValue)
                data.ttl = Ttl.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            else
                data.ttl = Defaults.Ttl.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            if (Timestamp.HasValue)
                data.timestamp = Timestamp.Value.ToUnixTime().ToString();
            else
                data.timestamp = now.ToUnixTime().ToString();
            data.CalculateMac(keyValue);
            
            return data;
        }
    }
}