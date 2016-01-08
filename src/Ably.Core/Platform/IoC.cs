using Ably.Transport;
using System;

namespace Ably.Platform
{
    public static class IoC
    {
        public static string getConnectionString()
        {
            throw new NotImplementedException();
        }

        public static ICrypto crypto
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public static ITransportFactory webSockets
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}