using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
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
        
        internal ApiKey(string appId)
        {
            _AppId = appId;
        }

        private ApiKey() { }

        public static ApiKey Parse(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                new ArgumentException("Ably key was not specified").Throw();

            var parts = key.Trim().Split(':');
            if (parts.Length != 3)
                new ArgumentOutOfRangeException("Ably key must contain three parts").Throw();

            return new ApiKey() { _AppId = parts[0], _KeyId = parts[1], _KeyValue = parts[2] };
        }
    }
}
