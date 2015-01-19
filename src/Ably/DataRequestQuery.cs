using System.Collections.Specialized;
using System.Text.RegularExpressions;
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

    public enum StatsUnit
    {
        Hour,
        Day,
        Month
    }

    public class StatsDataRequestQuery : DataRequestQuery
    {
        public StatsUnit? Unit { get; set; }

        public override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var result = new List<KeyValuePair<string, string>>(base.GetParameters());
            if (Unit.HasValue)
                result.Add(new KeyValuePair<string, string>("unit", Unit.ToString().ToLower()));
            return result;
        }

        public static StatsDataRequestQuery Create()
        {
            return new StatsDataRequestQuery();
        }
    }

    public class DataRequestQuery
    {
        protected bool Equals(DataRequestQuery other)
        {
            return Start.Equals(other.Start) 
                && End.Equals(other.End) 
                && Limit == other.Limit 
                && Direction == other.Direction 
                && ExtraParameters.SequenceEqual(other.ExtraParameters);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Start.GetHashCode();
                hashCode = (hashCode*397) ^ End.GetHashCode();
                hashCode = (hashCode*397) ^ Limit.GetHashCode();
                hashCode = (hashCode*397) ^ (int) Direction;
                hashCode = (hashCode*397) ^ (ExtraParameters != null ? ExtraParameters.GetHashCode() : 0);
                return hashCode;
            }
        }

        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public int? Limit { get; set; }
        public QueryDirection Direction { get; set; }
        internal Dictionary<string, string> ExtraParameters { get; set; } 

        public DataRequestQuery()
        {
            ExtraParameters = new Dictionary<string, string>();
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

            result.AddRange(ExtraParameters);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DataRequestQuery) obj);
        }

        internal static DataRequestQuery Parse(string querystring)
        {
            var query = new DataRequestQuery();
            var queryParameters = querystring.ParseQueryString();
            foreach (var key in queryParameters.AllKeys)
            {
                switch (key.ToLower())
                {
                    case "start":
                        query.Start = ToDateTime(queryParameters[key]);
                        break;
                    case "end":
                        query.End = ToDateTime(queryParameters[key]);
                        break;
                    case "direction":
                        var direction = QueryDirection.Forwards;
                        if (Enum.TryParse(queryParameters[key], true, out direction))
                            query.Direction = direction;
                        break;
                    case "limit":
                        int limit = 0;
                        if (int.TryParse(queryParameters[key], out limit))
                            query.Limit = limit;
                        break;
                    default:
                        query.ExtraParameters.Add(key, queryParameters[key]);
                        break;
                }
            }
            return query;
        }

        internal static DataRequestQuery GetLinkQuery(NameValueCollection headers, string link)
        {
            var linkPattern = "\\s*<(.*)>;\\s*rel=\"(.*)\"";
            var linkHeaders = headers.GetValues("Link") ?? new string[] {};
            foreach (var header in linkHeaders)
            {
                var match = Regex.Match(header, linkPattern);
                if (match.Success && match.Groups[2].Value.Equals(link, StringComparison.InvariantCultureIgnoreCase))
                {
                    var url = match.Groups[1].Value;
                    var queryString = url.Split('?')[1];
                    return Parse(queryString);
                }
            }
            return null;
        }

        private static DateTimeOffset? ToDateTime(object value)
        {
            if (value == null)
                return null;

            try
            {
                long miliseconds = (long)Convert.ChangeType(value, TypeCode.Int64);
                return miliseconds.FromUnixTimeInMilliseconds();
            }
            catch (Exception)
            {
                return null;
            }
        }


    }
}
