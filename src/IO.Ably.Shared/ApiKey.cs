using System;
using System.Net;

namespace IO.Ably
{
    /// <summary>
    /// Internal class used to parse ApiKeys. The api key has the following parts {keyName}:{KeySecret}
    /// The app and key parts form the KeyId
    /// </summary>
    public class ApiKey
    {
        internal string AppId { get; private set; }
        public string KeyName { get; private set; }
        public string KeySecret { get; private set; }

        public override string ToString()
        {
            return $"{KeyName}:{KeySecret}";
        }

        private ApiKey() { }

        public static ApiKey Parse(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new AblyException("Ably key was empty. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);
            }

            var parts = key.Trim().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                var keyParts = parts[0].Split(".".ToCharArray());
                return new ApiKey() { AppId = keyParts[0], KeyName = keyParts[0] + "." + keyParts[1], KeySecret = parts[1] };
            }

            throw new AblyException("Invalid ably key. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);
        }
    }
}
