using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public enum HistoryBy
    {
        Message,
        Bundle,
        Hour
    }
    public class HistoryDataRequestQuery : DataRequestQuery
    {
        public HistoryBy? By { get; set; }

        public static HistoryDataRequestQuery Create()
        {
            return new HistoryDataRequestQuery();
        }

        public override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var result = new List<KeyValuePair<string,string>>(base.GetParameters());
            if(By.HasValue)
                result.Add(new KeyValuePair<string, string>("by", By.ToString().ToLower()));
            return result;
        }
    }

    public class DataRequestQuery
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int? Limit { get; set; }
        public QueryDirection Direction { get; set; }

        public DataRequestQuery()
        {
            Direction = QueryDirection.Backwards;
        }

        internal void Validate()
        {
            if (Limit.HasValue && (Limit < 0 || Limit > 10000))
                throw new AblyException("History query limit must be between 0 and 10000");

            if (Start.HasValue)
            {
                if (Start.Value < new DateTime(1970, 1, 1))
                    throw new AblyException("Start only supports dates after 1 January 1970");
            }

            if (End.HasValue)
                if (End.Value < new DateTime(1970, 1, 1))
                    throw new AblyException("End only supports dates after 1 January 1970");

            if (Start.HasValue && End.HasValue)
                if (End.Value < Start.Value)
                    throw new AblyException("End date should be after Start date");
        }

        public virtual IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var result = new List<KeyValuePair<string, string>>();
            if (Start.HasValue)
                result.Add(new KeyValuePair<string, string>("start", Start.Value.ToUnixTimeInMilliseconds().ToString()));

            if (End.HasValue)
                result.Add(new KeyValuePair<string, string>("end", End.Value.ToUnixTimeInMilliseconds().ToString()));

            result.Add(new KeyValuePair<string, string>("direction", Direction.ToString().ToLower()));
            if (Limit.HasValue)
                result.Add(new KeyValuePair<string, string>("limit", Limit.Value.ToString()));

            return result;
        }
    }
}
