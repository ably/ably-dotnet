using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public interface IChannel
    {
        void Publish(string name, object data);
        string Name { get; }
    }

    public class Channel : IChannel
    {
        public string Name { get; private set; }
        private readonly Rest _restClient;
        private readonly string basePath;

        internal Channel(Rest restClient, string name)
        {
            Name = name;
            _restClient = restClient;
            basePath = string.Format("/apps/{0}/channels/{1}", restClient.Options.AppId, name);
        }

        public void Publish(string name, object data)
        {
            var request = _restClient.CreatePostRequest(basePath + "/publish");

            request.Data = new ChannelPublishPayload { Name = name, Data = data };
            _restClient.ExecuteRequest(request);
        }
        
    }

    public class ChannelPublishPayload
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
