using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#if SILVERLIGHT
using SCS = Ably.Utils;
#else
using SCS = System.Collections.Specialized;
#endif

namespace Ably
{
    public enum StatsBy
    {
        Minute,
        Hour,
        Day,
        Month
    }

    /// <summary>
    /// Stats query. Allows you query for application statistics
    /// </summary>
    public class StatsDataRequestQuery : DataRequestQuery
    {
        /// <summary>
        /// Define how the stats will be aggregated and presented.
        /// </summary>
        public StatsBy? By { get; set; }

        public StatsDataRequestQuery()
        {
            By = StatsBy.Minute;
            Direction = QueryDirection.Forwards;
        }

        internal override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var result = new List<KeyValuePair<string, string>>(base.GetParameters());
            if (By.HasValue)
                result.Add(new KeyValuePair<string, string>("by", By.ToString().ToLower()));
            return result;
        }
    }

    internal static class DataRequestLinkType
    {
        public const string Current = "current";
        public const string Next = "next";
        public const string First = "first";
    }


    /// <summary>
    /// Data request query used for querying stats and history
    /// It makes it easier to pass parameters to the ably service by encapsulating the query string parameters passed
    /// </summary>
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

        /// <summary>
        /// Start of the query interval as UTC DateTimeOffset.
        /// Default: null
        /// </summary>
        public DateTimeOffset? Start { get; set; }
        /// <summary>
        /// End of the query interval as UTC DateTimeOffset
        /// Default: null
        /// </summary>
        public DateTimeOffset? End { get; set; }

        /// <summary>
        /// The number of the results returned by the server. If there are more result the NextQuery on the PaginatedResource will be populated
        /// Default: Uses <see cref="Config.Limit"/> which is 100.
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// Query directions. It determines the order in which results are returned. <see cref="QueryDirection"/>
        /// </summary>
        public QueryDirection Direction { get; set; }
        
        /// <summary>
        /// Used mainly when parsing query strings to hold extra parameters that need to be passed back to the service. 
        /// </summary>
        public Dictionary<string, string> ExtraParameters { get; set; }

        /// <summary>
        /// If the datasource was created by parsing a query string it can be accessed from here.
        /// It is mainly used for debugging purposes of Current and NextQueries of PaginatedResources
        /// </summary>
        public string QueryString { get; private set; }

        public bool IsEmpty
        {
            get { return QueryString.IsEmpty(); }
        }

        /// <summary>
        /// An empty Query
        /// </summary>
        public readonly static DataRequestQuery Empty = new DataRequestQuery();


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

        internal virtual IEnumerable<KeyValuePair<string, string>> GetParameters()
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
            query.QueryString = querystring;
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

        internal static DataRequestQuery GetLinkQuery(SCS.NameValueCollection headers, string link)
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
            return Empty;
        }


        private static DateTimeOffset? ToDateTime(object value)
        {
            if (value == null)
                return null;

            try
            {
                long miliseconds = (long)Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
                return miliseconds.FromUnixTimeInMilliseconds();
            }
            catch (Exception)
            {
                return null;
            }
        }


    }
}
