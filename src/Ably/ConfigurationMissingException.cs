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
    public class ConfigurationMissingException : ConfigurationErrorsException
    {
        public ConfigurationMissingException(string message)
            : base(message)
        {

        }
    }
}
