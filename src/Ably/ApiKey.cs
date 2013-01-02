using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
        private ApiKey()
        {

        }

        public static ApiKey Parse(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new AblyInvalidApiKeyException(key);

            var parts = key.Trim().Split(':');
            if (parts.Length != 3)
                throw new AblyInvalidApiKeyException(key);

            return new ApiKey() { _AppId = parts[0], _KeyId = parts[1], _KeyValue = parts[2] };
        }
    }
}
