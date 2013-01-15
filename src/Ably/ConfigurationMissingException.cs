using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

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
