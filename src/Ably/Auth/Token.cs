using System;
using Newtonsoft.Json.Linq;

namespace Ably.Auth
{
    public sealed class Token
    {
        public string Id { get; private set; }
        public string KeyId { get; internal set; }
        
        public DateTime ExpiresAt { get; internal set;}
        public DateTime IssuedAt { get; internal set; }
        public Capability Capability { get; internal set; }
        public string ClientId { get; internal set; }

        public Token()
        {
        }

        public Token(string id)
        {
            Id = id;
        }

        public static Token FromJson(JObject json)
        {
            if (json == null)
                return new Token();
            var token = new Token();
            token.Id = (string) json["id"];
            token.KeyId = (string) json["key"];
            token.ExpiresAt = ((long) json["expires"]).FromUnixTime();
            token.IssuedAt = ((long) json["issued_at"]).FromUnixTime();
            token.Capability = new Capability(json["capability"].ToString());
            token.ClientId = (string) json["client_id"];
            return token;
        }

        public static bool IsToken(JObject json)
        {
            return json != null && json["issued_at"] != null;
        }
    }
}
