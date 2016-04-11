using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IO.Ably.Tests
{
    [CollectionDefinition("AblyRest SandBox Collection")]
    public class AblyCollectionFixture : ICollectionFixture<AblySandboxFixture>
    {
        
    }

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

        public string FirstValidKey => Keys.FirstOrDefault()?.KeyStr;

        public string RestHost => Environment.ToString().ToLower() + "-" + Config.DefaultHost;

        public int RestPort { get; set; }
        public bool Tls { get; set; }
        public AblyEnvironment Environment => AblyEnvironment.Sandbox;

        public TestEnvironmentSettings()
        {
            Keys = new List<Key>();
        }

        internal AblyHttpClient GetHttpClient()
        {
            return new AblyHttpClient(RestHost, null, Tls, null);
        }

        public ClientOptions CreateDefaultOptions()
        {
            var opts = new ClientOptions() { Key = FirstValidKey};
            FillInOptions(opts);
            return opts;
        }
        public void FillInOptions(ClientOptions opts)
        {
            opts.RestHost = RestHost;
            opts.Port = RestPort;
            opts.Tls = Tls;
        }
    }
}