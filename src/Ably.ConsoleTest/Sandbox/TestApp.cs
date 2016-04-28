﻿using System.Linq;
using Newtonsoft.Json.Linq;

namespace IO.Ably.ConsoleTest.Sandbox
{
    /// <summary>This readonly class represents test application created in the sandbox.</summary>
    public class TestApp
    {
        const AblyEnvironment environment = AblyEnvironment.Sandbox;

        class Key
        {
            public readonly string keyName;
            public readonly string keySecret;
            public readonly string keyStr;
            public readonly string capability;

            internal Key( string appId, JToken key )
            {
                keyName = appId + "." + (string)key[ "keyName" ];
                keySecret = (string)key[ "keySecret" ];
                keyStr = (string)key[ "keyStr" ];
                capability = (string)key[ "capability" ];
            }
        }

        readonly string appId;
        readonly Key[] keys;
        readonly bool tls;

        internal TestApp( bool tls, JObject json )
        {
            this.tls = tls;
            appId = (string)json[ "appId" ];
            keys = json[ "keys" ]
                .Select( jt => new Key( appId, jt ) )
                .ToArray();
        }

        /// <summary>Construct <see cref="ClientOptions" /> targeting this sandbox application.</summary>
        public ClientOptions ToAblyOptions()
        {
            ClientOptions res = new ClientOptions();
            res.Key = keys[ 0 ].keyStr;
            res.Environment = environment;
            res.Tls = tls;
            res.UseBinaryProtocol = false;
            return res;
        }
    }
}