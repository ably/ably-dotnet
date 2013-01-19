using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public interface IChannel
    {
        string Name { get; }
    }

    public class Channel : IChannel
    {
        public string Name { get; private set; }
        private readonly Rest _RestClient;
        private readonly string basePath;

        internal Channel(Rest restClient, string name)
        {
            Name = name;
            _RestClient = restClient;
            basePath = string.Format("{0}/apps/{1}/channels/{2}", Config.DefaultHost, restClient.Options.AppId, name);
        }

        public void Publish(string name, object data)
        {
            
        }
        
    }
}
