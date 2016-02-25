using System;
using System.Net;

namespace IO.Ably
{
    /// <summary>
    /// Internal class used to parse ApiKeys. The api key has the following parts {app}.{key}:{KeyValue} 
    /// The app and key parts form the KeyId
    /// </summary>
    public class ApiKey
    {
        private string _KeySecret;
        private string _KeyName;
        private string _AppId;

        public string AppId
        {
            get { return _AppId; }
        }

        public string KeyName
        {
            get { return _KeyName; }
        }

        public string KeySecret
        {
            get { return _KeySecret; }
        }
        
        private ApiKey() { }
        public override string ToString()
        {
            return string.Format("{0}:{1}", KeyName, KeySecret);
        }

        public static ApiKey Parse(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new AblyException("Ably key was empty. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);

            var parts = key.Trim().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                var keyParts = parts[0].Split(".".ToCharArray());
                return new ApiKey() { _AppId = keyParts[0], _KeyName = keyParts[0] + "." + keyParts[1], _KeySecret = parts[1] };
            }

            throw new AblyException("Invalid ably key. Ably key must be in the following format [AppId].[keyId]:[keyValue]", 40101, HttpStatusCode.Unauthorized);
        }
    }
}
