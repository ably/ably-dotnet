using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace IO.Ably
{
    public enum StatsIntervalGranularity
    {
        Minute,
        Hour,
        Day,
        Month
    }

    /// <summary>
    /// Stats query. Allows you query for application statistics
    /// </summary>
    public class StatsRequestParams : HistoryRequestParams
    {
        /// <summary>
        /// Define how the stats will be aggregated and presented.
        /// </summary>
        public StatsIntervalGranularity? Unit { get; set; }

        public StatsRequestParams()
        {
            Unit = StatsIntervalGranularity.Minute;
            Direction = QueryDirection.Backwards;
            Limit = Defaults.QueryLimit;
        }

        internal override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var result = new List<KeyValuePair<string, string>>(base.GetParameters());
            if (Unit.HasValue)
            {
                result.Add(new KeyValuePair<string, string>("by", Unit.ToString().ToLower()));
            }

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
    public class HistoryRequestParams
    {
        protected bool Equals(HistoryRequestParams other)
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
                hashCode = (hashCode * 397) ^ End.GetHashCode();
                hashCode = (hashCode * 397) ^ Limit.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Direction;
                hashCode = (hashCode * 397) ^ (ExtraParameters != null ? ExtraParameters.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Start of the query interval as UTC Date.
        /// Default: null
        /// </summary>
        public DateTimeOffset? Start { get; set; }
        /// <summary>
        /// End of the query interval as UTC Date
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
            get { return StringExtensions.IsEmpty(QueryString); }
        }

        public static readonly HistoryRequestParams Empty = new HistoryRequestParams();


        public HistoryRequestParams()
        {
            ExtraParameters = new Dictionary<string, string>();
            Direction = QueryDirection.Backwards;
        }

        internal void Validate()
        {
            if (Limit.HasValue && (Limit < 0 || Limit > 1000))
            {
                throw new AblyException("History query limit must be between 0 and 1000");
            }

            if (Start.HasValue)
            {
                if (Start.Value < DateExtensions.Epoch)
                {
                    throw new AblyException("Start only supports dates after 1 January 1970");
                }
            }

            if (End.HasValue)
            {
                if (End.Value < DateExtensions.Epoch)
                {
                    throw new AblyException("End only supports dates after 1 January 1970");
                }
            }

            if (Start.HasValue && End.HasValue)
            {
                if (End.Value < Start.Value)
                {
                    throw new AblyException("End date should be after Start date");
                }
            }
        }

        internal virtual IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var result = new List<KeyValuePair<string, string>>();
            if (Start.HasValue)
            {
                result.Add(new KeyValuePair<string, string>("start", Start.Value.ToUnixTimeInMilliseconds().ToString()));
            }

            if (End.HasValue)
            {
                result.Add(new KeyValuePair<string, string>("end", End.Value.ToUnixTimeInMilliseconds().ToString()));
            }

            result.Add(new KeyValuePair<string, string>("direction", Direction.ToString().ToLower()));
            if (Limit.HasValue)
            {
                result.Add(new KeyValuePair<string, string>("limit", Limit.Value.ToString()));
            }
            else
            {
                result.Add(new KeyValuePair<string, string>("limit","100"));
            }

            result.AddRange(ExtraParameters);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((HistoryRequestParams)obj);
        }

        internal static HistoryRequestParams Parse(string querystring)
        {
            var query = new HistoryRequestParams();
            query.QueryString = querystring;
            HttpValueCollection queryParameters = HttpUtility.ParseQueryString(querystring);
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
                        {
                            query.Direction = direction;
                        }

                        break;
                    case "limit":
                        int limit = 0;
                        if (int.TryParse(queryParameters[key], out limit))
                        {
                            query.Limit = limit;
                        }

                        break;
                    default:
                        query.ExtraParameters.Add(key, queryParameters[key]);
                        break;
                }
            }
            return query;
        }

        internal static HistoryRequestParams GetLinkQuery(HttpHeaders headers, string link)
        {
            var linkPattern = "\\s*<(.*)>;\\s*rel=\"(.*)\"";
            IEnumerable<string> linkHeaders;
            if (headers.TryGetValues("Link", out linkHeaders))
            {
                foreach (var header in linkHeaders)
                {
                    var match = Regex.Match(header, linkPattern);
                    if (match.Success && match.Groups[2].Value.Equals(link, StringComparison.OrdinalIgnoreCase))
                    {
                        var url = match.Groups[1].Value;
                        var queryString = url.Split('?')[1];
                        return Parse(queryString);
                    }
                }
            }
            return Empty;
        }

        private static DateTimeOffset? ToDateTime(object value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                long miliseconds = (long)Convert.ChangeType(value, typeof(long));
                return miliseconds.FromUnixTimeInMilliseconds();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
