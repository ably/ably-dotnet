using System.Collections.Generic;
using System.Net.Http.Headers;

namespace IO.Ably
{
    public class PaginatedResult<T> : List<T>
    {
        private readonly int _limit;

        public PaginatedResult() : this(null, Defaults.QueryLimit)
        {

        }

        public PaginatedResult(HttpHeaders headers, int limit)
        {
            _limit = limit;
            if (null != headers)
            {
                CurrentQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Current);
                NextQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Next);
                FirstQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.First);
            }
        }

        public bool HasNext => null != NextQuery && NextQuery.IsEmpty == false;

        public DataRequestQuery NextQuery { get; }
        public DataRequestQuery FirstQuery { get; private set; }
        public DataRequestQuery CurrentQuery { get; private set; }
    }
}