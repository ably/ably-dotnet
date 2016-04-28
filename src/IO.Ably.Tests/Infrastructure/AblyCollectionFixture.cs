using System.Collections.Generic;
using System.Linq;
using IO.Ably.Transport;
using Xunit;

namespace IO.Ably.Tests
{
    [CollectionDefinition("AblyRest SandBox Collection")]
    public class AblyCollectionFixture : ICollectionFixture<AblySandboxFixture> { }

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

        public bool Tls { get; set; }
        public AblyEnvironment Environment => AblyEnvironment.Sandbox;
        public CipherParams CipherParams { get; set; }

        public TestEnvironmentSettings()
        {
            Keys = new List<Key>();
        }

        internal AblyHttpClient GetHttpClient()
        {
            var ablyHttpOptions = new AblyHttpOptions() { IsSecure = Tls};
            ablyHttpOptions.Host = CreateDefaultOptions().FullRestHost();
            return new AblyHttpClient(ablyHttpOptions);
        }

        public ClientOptions CreateDefaultOptions()
        {
            return new ClientOptions() { Key = FirstValidKey, Tls = Tls, Environment = Environment };
        }
    }
}