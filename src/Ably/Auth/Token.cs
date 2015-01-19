using System;
using Newtonsoft.Json.Linq;

namespace Ably.Auth
{
    public sealed class Token
    {
        public string Id { get; private set; }
        public string KeyId { get; internal set; }
        
        public DateTimeOffset ExpiresAt { get; internal set;}
        public DateTimeOffset IssuedAt { get; internal set; }
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
            if(json["expires"] != null)
                token.ExpiresAt = ((long) json["expires"]).FromUnixTime();
            if(json["issued_at"] != null)
                token.IssuedAt = ((long) json["issued_at"]).FromUnixTime();
            if(json["capability"] != null)
                token.Capability = new Capability(json["capability"].ToString());
            token.ClientId = (string) json["clientId"];
            return token;
        }

        public static bool IsToken(JObject json)
        {
            return json != null && json["issued_at"] != null;
        }
    }
}
