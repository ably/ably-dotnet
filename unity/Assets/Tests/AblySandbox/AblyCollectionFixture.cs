﻿using System.Collections.Generic;
using System.Linq;
using IO.Ably;

namespace Assets.Tests.AblySandbox
{
    public class Key
    {
        public string KeyName { get; set; }

        public string KeySecret { get; set; }

        public string KeyStr { get; set; }

        public string Capability { get; set; }
    }

    public class TestEnvironmentSettings
    {
        public string AppId { get; set; }

        public List<Key> Keys { get; set; }

        public const string PresenceChannelName = "persisted:presence_fixtures";

        public string FirstValidKey => Keys.FirstOrDefault()?.KeyStr;

        public string KeyWithChannelLimitations => Keys[2].KeyStr;

        public bool Tls { get; set; }

        public string Environment { get; set; } = "sandbox";

        public CipherParams CipherParams { get; set; }

        public TestEnvironmentSettings()
        {
            Keys = new List<Key>();
        }

        public ClientOptions CreateDefaultOptions(string key = null, string environment = null)
        {
            environment ??= Environment;

            var ablyEnv = System.Environment.GetEnvironmentVariable("ABLY_ENV");
            var env = !string.IsNullOrEmpty(ablyEnv) ? ablyEnv.Trim() : environment;

            return new ClientOptions { Key = key ?? FirstValidKey, Tls = Tls, Environment = env, AutomaticNetworkStateMonitoring = false};
        }

        internal AblyHttpClient GetHttpClient(string environment = null)
        {
            var ablyHttpOptions = new AblyHttpOptions { IsSecure = Tls };
            ablyHttpOptions.Host = CreateDefaultOptions(null, environment).FullRestHost();
            return new AblyHttpClient(ablyHttpOptions);
        }
    }
}
