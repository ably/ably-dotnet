using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public interface IChannel
    {
        void Publish(string name, object data);
        IEnumerable<Message> History();
        IEnumerable<Message> History(DataRequestQuery query);
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

            request.Data = new ChannelPublishPayload { Name = name, Data = data };
            _restClient.ExecuteRequest(request);
        }

        public IEnumerable<Message> History()
        {
            return History(new DataRequestQuery());
        }

        public IEnumerable<Message> History(DataRequestQuery query)
        {
            ValidateQuery(query);

            var request = _restClient.CreateGetRequest(basePath + "/history");

            if (query.Start.HasValue)
                request.QueryParameters.Add("start", query.Start.Value.ToUnixTime().ToString());

            if (query.End.HasValue)
                request.QueryParameters.Add("end", query.End.Value.ToUnixTime().ToString());

            request.QueryParameters.Add("direction", query.Direction.ToString().ToLower());
            if (query.Limit.HasValue)
                request.QueryParameters.Add("limit", query.Limit.Value.ToString());

            _restClient.ExecuteRequest(request);
            return new List<Message>();
        }

        private void ValidateQuery(DataRequestQuery query)
        {
            if (query.Limit.HasValue && (query.Limit < 0 || query.Limit > 10000))
                new ArgumentOutOfRangeException("Limit", "History query limit must be between 0 and 10000").Throw();

            if(query.Start.HasValue)
            {
                if (query.Start.Value < new DateTime(1970, 1, 1))
                    new ArgumentOutOfRangeException("Start", "Start only supports dates after 1 January 1970").Throw();
            }

            if(query.End.HasValue)
                if (query.End.Value < new DateTime(1970, 1, 1))
                    new ArgumentOutOfRangeException("End", "End only supports dates after 1 January 1970").Throw();

            if (query.Start.HasValue && query.End.HasValue)
                if (query.End.Value < query.Start.Value)
                    new ArgumentOutOfRangeException("End", "End date should be after Start date").Throw(); 
        }

        public Stats Stats()
        {
            return Stats(new DataRequestQuery());
        }

        public Stats Stats(DataRequestQuery query)
        {
            ValidateQuery(query);

            var request = _restClient.CreateGetRequest(basePath + "/stats");

            if (query.Start.HasValue)
                request.QueryParameters.Add("start", query.Start.Value.ToUnixTime().ToString());

            if (query.End.HasValue)
                request.QueryParameters.Add("end", query.End.Value.ToUnixTime().ToString());

            request.QueryParameters.Add("direction", query.Direction.ToString().ToLower());
            if (query.Limit.HasValue)
                request.QueryParameters.Add("limit", query.Limit.Value.ToString());

            _restClient.ExecuteRequest(request);

            return new Stats() ;
        }
    }

    public class Message
    {
        public string Name { get; set; }
        public string ChannelId { get; set; }
        public object Data { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

    public class DataRequestQuery
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int? Limit { get; set; }
        public HistoryDirection Direction { get; set; }

        public DataRequestQuery()
        {
            Direction = HistoryDirection.Backwards;
        }
    }

    public enum HistoryDirection
    {
        Forwards,
        Backwards
    }

    public class ChannelPublishPayload
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
