using System;
using IO.Ably;

namespace IO.Ably
{
    internal class AblyHttpOptions
    {
        internal bool IsSecure { get; set; }
        internal string Host { get; set; }
        internal int? Port { get; set;  }
        internal TimeSpan DisconnectedRetryTimeout { get; set; }
        internal TimeSpan SuspendedRetryTimeout { get; set; }
        internal TimeSpan HttpOpenTimeout { get; set; }
        internal TimeSpan HttpRequestTimeout { get; set;  }
        internal int HttpMaxRetryCount { get; set; }
        internal TimeSpan HttpMaxRetryDuration { get; set; }
        public bool IsDefaultHost { get; set; } = true;

        internal Func<DateTimeOffset> NowFunc { get; set; }

        public ILogger Logger { get; set; }

        public AblyHttpOptions() //Used for testing
        {
            Host = Defaults.RestHost;
            
            //Setting up some defaults
            DisconnectedRetryTimeout = TimeSpan.FromSeconds(15);
            SuspendedRetryTimeout = TimeSpan.FromSeconds(30);
            HttpOpenTimeout = TimeSpan.FromSeconds(4);
            HttpRequestTimeout = TimeSpan.FromSeconds(15);
            HttpMaxRetryCount = 3;
            HttpMaxRetryDuration = TimeSpan.FromSeconds(10);

            NowFunc = Defaults.NowFunc();
            Logger = IO.Ably.DefaultLogger.LoggerInstance;
        }

        public AblyHttpOptions(ClientOptions options)
        {
            IsSecure = options.Tls;
            Port = options.Tls ? options.TlsPort : options.Port;
            Host = options.FullRestHost();
            IsDefaultHost = options.IsDefaultRestHost;
            DisconnectedRetryTimeout = options.DisconnectedRetryTimeout;
            SuspendedRetryTimeout = options.SuspendedRetryTimeout;
            HttpOpenTimeout = options.HttpOpenTimeout;
            HttpRequestTimeout = options.HttpRequestTimeout;
            HttpMaxRetryCount = options.IsDefaultRestHost ? options.HttpMaxRetryCount : 1;
            HttpMaxRetryDuration = options.HttpMaxRetryDuration;

            NowFunc = options.NowFunc;
            Logger = options.Logger;
        }
    }
}