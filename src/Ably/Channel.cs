using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public interface IChannel
    {
        void Publish(string name, object data);
        IEnumerable<Message> History();
        IEnumerable<Message> History(HistoryDataRequestQuery query);
        Stats Stats();
        Stats Stats(DataRequestQuery query);
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

            request.PostData = GetPostData(name, data);
            _restClient.ExecuteRequest(request);
        }

        private static ChannelPublishPayload GetPostData(string name, object data)
        {
            ChannelPublishPayload payload = new ChannelPublishPayload { Name = name};
            if(data is byte[])
            {
                payload.Data = Convert.ToBase64String((byte[])data);
                payload.Encoding = "base64";
            }
            else
            {
                payload.Data = data;
            }

            return payload;
        }

        public IEnumerable<Message> History()
        {
            return History(new HistoryDataRequestQuery());
        }

        public IEnumerable<Message> History(HistoryDataRequestQuery query)
        {
            query.Validate();

            var request = _restClient.CreateGetRequest(basePath + "/history");

            if (query.Start.HasValue)
                request.QueryParameters.Add("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());

            if (query.End.HasValue)
                request.QueryParameters.Add("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());

            request.QueryParameters.Add("direction", query.Direction.ToString().ToLower());
            if (query.Limit.HasValue)
                request.QueryParameters.Add("limit", query.Limit.Value.ToString());
            if (query.By.HasValue)
                request.QueryParameters.Add("by", query.By.Value.ToString().ToLower());

            var response = _restClient.ExecuteRequest(request);

            return ParseHistoryResponse(response);
        }

        private IEnumerable<Message> ParseHistoryResponse(AblyResponse response)
        {
            var results = new List<Message>();
            if (response == null)
                return results;

            var json = JArray.Parse(response.JsonResult);
            foreach (var message in json)
            {
                results.Add(new Message
                {
                    Name = (string)message["name"],
                    Data = GetMessageData(message),
                    TimeStamp = ((long)message["timestamp"]).FromUnixTimeInMilliseconds(),
                    ChannelId = (string)message["client_id"]
                });
            }
            return results;
        }

        private object GetMessageData(JToken message)
        {
            var enconding = (string)message["encoding"];

            if(enconding.IsNotEmpty() && enconding == "base64")
            {
                return Convert.FromBase64String((string)message["data"]);
            }
            return message["data"] != null ? message["data"].ToString() : null ;
        }

        public Stats Stats()
        {
            return Stats(new DataRequestQuery());
        }

        public Stats Stats(DataRequestQuery query)
        {
            query.Validate();

            var request = _restClient.CreateGetRequest(basePath + "/stats");

            if (query.Start.HasValue)
                request.QueryParameters.Add("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());

            if (query.End.HasValue)
                request.QueryParameters.Add("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());

            request.QueryParameters.Add("direction", query.Direction.ToString().ToLower());
            if (query.Limit.HasValue)
                request.QueryParameters.Add("limit", query.Limit.Value.ToString());

            _restClient.ExecuteRequest(request);

            return new Stats() ;
        }
    }

    public class ChannelPublishPayload
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("data")]
        public object Data { get; set; }
        [JsonProperty("encoding", NullValueHandling= NullValueHandling.Ignore)]
        public string Encoding { get; set; }
    }
}
