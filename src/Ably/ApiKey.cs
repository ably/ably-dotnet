using System;
using System.Net;

namespace Ably
{
    /// <summary>
    /// Internal class used to parse ApiKeys
    /// </summary>
    public class ApiKey
    {
        private string _KeyValue;
        private string _KeyId;
        private string _AppId;

        public string AppId
        {
            get { return _AppId; }
        }

        public string KeyId
        {
            get { return _KeyId; }
        }

        public string KeyValue
        {
            get { return _KeyValue; }
        }
        
        private ApiKey() { }
        public override string ToString()
        {
            return string.Format("{0}:{1}", KeyId, KeyValue);
        }

        public static ApiKey Parse(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new AblyException("Ably key was empty. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);

            var parts = key.Trim().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                var keyParts = parts[0].Split(".".ToCharArray());
                return new ApiKey() { _AppId = keyParts[0], _KeyId = keyParts[0] + "." + keyParts[1], _KeyValue = parts[1] };
            }

            throw new AblyException("Invalid ably key. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);
        }
    }
}
