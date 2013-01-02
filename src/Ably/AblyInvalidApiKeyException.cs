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
    public class AblyInvalidApiKeyException : AblyException
    {
        private readonly string _ApiKey;

        public string ApiKey
        {
            get { return _ApiKey; }
        }


        public AblyInvalidApiKeyException(string apiKey)
        {
            _ApiKey = apiKey;
        }

        public override string Message
        {
            get
            {
                return String.Format("The provided key '{0}' is invalid. For more info about ably api keys please visit ....", ApiKey); //TODO: Links to help with info
            }
        }


        protected AblyInvalidApiKeyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

    }
}
