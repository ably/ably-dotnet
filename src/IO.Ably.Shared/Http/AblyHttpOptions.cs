﻿using System;
using System.Collections.Generic;

namespace IO.Ably
{
    internal class AblyHttpOptions
    {
        internal bool IsSecure { get; set; }

        internal string Host { get; set; }

        internal int? Port { get; set; }

        internal TimeSpan DisconnectedRetryTimeout { get; set; }

        internal TimeSpan SuspendedRetryTimeout { get; set; }

        internal TimeSpan HttpOpenTimeout { get; set; }

        internal TimeSpan HttpRequestTimeout { get; set; }

        internal int HttpMaxRetryCount { get; set; }

        internal TimeSpan HttpMaxRetryDuration { get; set; }

        internal TimeSpan FallbackRetryTimeOut { get; set; }

        public bool IsDefaultHost { get; set; } = true;

        internal string[] FallbackHosts { get; set; }

        internal bool FallbackHostsUseDefault { get; set; }

        internal Func<DateTimeOffset> NowFunc { get; set; }

        public ILogger Logger { get; set; }

        public bool AddRequestIds { get; set; }

        public Dictionary<string, string> Agents { get; set; }

        public AblyHttpOptions()
        {
            // Used for testing
            Host = Defaults.RestHost;

            // Setting up some defaults
            DisconnectedRetryTimeout = Defaults.DisconnectedRetryTimeout;
            SuspendedRetryTimeout = Defaults.SuspendedRetryTimeout;
            HttpOpenTimeout = Defaults.MaxHttpOpenTimeout;
            HttpRequestTimeout = Defaults.MaxHttpRequestTimeout;
            HttpMaxRetryCount = Defaults.HttpMaxRetryCount;
            HttpMaxRetryDuration = Defaults.HttpMaxRetryDuration;
            FallbackRetryTimeOut = Defaults.FallbackRetryTimeout;
            FallbackHosts = Defaults.FallbackHosts;
            FallbackHostsUseDefault = false;

            NowFunc = Defaults.NowFunc();
            Logger = DefaultLogger.LoggerInstance;
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
            HttpMaxRetryCount = options.HttpMaxRetryCount;
            HttpMaxRetryDuration = options.HttpMaxRetryDuration;
            FallbackRetryTimeOut = options.FallbackRetryTimeout;
            FallbackHosts = options.GetFallbackHosts();
            FallbackHostsUseDefault = options.FallbackHostsUseDefault;
            AddRequestIds = options.AddRequestIds;
            Agents = options.Agents;

            NowFunc = options.NowFunc;
            Logger = options.Logger;
        }
    }
}
