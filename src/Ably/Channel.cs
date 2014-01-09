using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Ably
{
    public class Channel : IChannel
    {
        public string Name { get; private set; }
        private readonly Rest _restClient;
        private readonly string basePath;

        internal Channel(Rest restClient, string name)
        {
            Name = name;
            _restClient = restClient;
            basePath = string.Format("/channels/{0}", name);
        }

        public void Publish(string name, object data)
        {
            var request = _restClient.CreatePostRequest(basePath + "/publish");

            request.PostData = GetPostData(name, data);
            _restClient.ExecuteRequest(request);
        }

        public void Publish(IEnumerable<Message> messages)
        {
            throw new NotImplementedException();
        }

        public IList<PresenceMessage> Presence()
        {
            var request = _restClient.CreateGetRequest(basePath + "/presence");
            var response = _restClient.ExecuteRequest(request);

            return JsonConvert.DeserializeObject<List<PresenceMessage>>(response.JsonResult);
        }

        private static ChannelPublishPayload GetPostData(string name, object data)
        {
            var payload = new ChannelPublishPayload { Name = name};
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

        public IPartialResult<Message> History()
        {
            return History(new HistoryDataRequestQuery());
        }


        public IPartialResult<Message> History(DataRequestQuery query)
        {
            query.Validate();

            var request = _restClient.CreateGetRequest(basePath + "/history");

            request.AddQueryParameters(query.GetParameters());

            var response = _restClient.ExecuteRequest(request);

            return ParseHistoryResponse(response, query);
        }

        public IPartialResult<Message> History(HistoryDataRequestQuery query)
        {
            return History(query as DataRequestQuery);
        }

        private IPartialResult<Message> ParseHistoryResponse(AblyResponse response, DataRequestQuery request)
        {
            Contract.Assert(response != null);

            var results = new PartialResult<Message>(request.Limit ?? 100);
            if (response.JsonResult.IsEmpty())
                return results;

            var json = JArray.Parse(response.JsonResult);
            foreach (var message in json)
            {
                results.Add(new Message
                {
                    Name = message.OptValue<string>("name"),
                    Data = GetMessageData(message),
                    TimeStamp = message.OptValue<long>("timestamp").FromUnixTimeInMilliseconds(),
                    ChannelId = message.OptValue<string>("client_id")
                });
            }
            results.NextQuery = DataRequestQuery.GetLinkQuery(response.Headers, "next");
            results.InitialResultQuery = DataRequestQuery.GetLinkQuery(response.Headers, "first");
            return results;
        }

        private object GetMessageData(JToken message)
        {
            var enconding = (string)message["encoding"];

            if(enconding.IsNotEmpty() && enconding == "base64")
            {
                return Convert.FromBase64String((string)message["data"]);
            }
            return message["data"];
        }

        public IList<Stats> Stats()
        {
            return Stats(new DataRequestQuery());
        }

        public IList<Stats> Stats(DataRequestQuery query)
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

            return new List<Stats>();
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
