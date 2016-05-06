using System;
using IO.Ably.Realtime;

namespace IO.Ably.Transport
{
    internal sealed class ConnectionAttemptsInfo
    {
        internal DateTimeOffset? FirstAttempt { get; private set; }
        private readonly ClientOptions _options;
        private readonly Connection _connection;
        internal int NumberOfAttempts { get; private set; }
        private object _syncLock = new object();

        public ConnectionAttemptsInfo(ClientOptions options, Connection connection)
        {
            if(options == null)
                throw new ArgumentNullException(nameof(options));

            if(connection == null)
                throw new ArgumentNullException(nameof(connection));


            _options = options;
            _connection = connection;
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                NumberOfAttempts = 0;
                FirstAttempt = null;
            }
        }

        public bool ShouldSuspend()
        {
            lock (_syncLock)
            {
                if (FirstAttempt == null)
                    return false;
                return (Config.Now() - FirstAttempt.Value) >= _connection.ConnectionStateTtl;
            }
        }

        public void Increment()
        {
            lock (_syncLock)
            {
                if (NumberOfAttempts == 0)
                {
                    FirstAttempt = Config.Now();
                }

                NumberOfAttempts++;
            }
        }
    }
}