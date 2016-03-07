using System.Collections.Generic;
using System.Net;

namespace IO.Ably
{
    public class PaginatedResource<T> : List<T>
    {
        private readonly int _limit;

        public PaginatedResource() : this(Config.Limit)
        {}

        public PaginatedResource(int limit)
        {
            _limit = limit;
        }

        public bool HasNext { get { return NextQuery.IsEmpty == false; } }
        public DataRequestQuery NextQuery { get; set; }
        public DataRequestQuery FirstQuery { get; set; }
        public DataRequestQuery CurrentQuery { get; set; }
    }

    internal static class PaginatedResource
    {
        public static PaginatedResource<T> InitialisePartialResult<T>( WebHeaderCollection headers, int? limit = null)
        {
            var result = new PaginatedResource<T>(limit ?? Config.Limit);
            result.CurrentQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Current);
            result.NextQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Next);
            result.FirstQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.First);
            return result;
        }
    }
}