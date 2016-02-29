using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace IO.Ably.AcceptanceTests
{
    public class TestVars
    {
        public String appId;
        public List<Key> keys;
        public JObject TestAppSpec;

        public string restHost
        {
            get { return Environment.ToString().ToLower() + "-" + Config.DefaultHost; }
        }
        public int? restPort;
        public bool tls;
        internal AblyEnvironment Environment;

        public AblyOptions CreateOptions(string key) {
			var opts = new AblyOptions() { Key = key};
			FillInOptions(opts);
			return opts;
		}
		public void FillInOptions(AblyOptions opts) {
			opts.Host = restHost;
			opts.Port = restPort;
			opts.Tls = tls;
		}
    }
}
