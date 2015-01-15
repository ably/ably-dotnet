using System;
using System.Collections.Generic;

namespace Ably.AcceptanceTests
{
    public class TestVars
    {
        public String appId;
        public List<Key> keys;
        public String restHost;
        public int? restPort;
        public bool tls;

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
