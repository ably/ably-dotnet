using System.Collections.Generic;

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
    public class StatsRequestParams : PaginatedRequestParams
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
}
