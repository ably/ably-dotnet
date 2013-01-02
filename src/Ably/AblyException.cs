using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Ably
{
    public class AblyException : Exception
    {
        public AblyException()
        {

        }
        public AblyException(string message)
            : base(message)
        {

        }
        public AblyException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected AblyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

    }
}
