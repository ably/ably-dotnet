using System.Collections.Generic;
using System.Net.Http.Headers;

namespace IO.Ably
{
    public class PaginatedResource<T> : List<T>
    {
        private readonly int _limit;

        public PaginatedResource() : this(null, Config.Limit)
        {
        }

        public PaginatedResource(HttpHeaders headers, int limit)
        {
            _limit = limit;
            if (null != headers)
            {
                CurrentQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Current);
                NextQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Next);
                FirstQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.First);
            }
        }

        public bool HasNext
        {
            get { return null != NextQuery && NextQuery.IsEmpty == false; }
        }

        public DataRequestQuery NextQuery { get; }
        public DataRequestQuery FirstQuery { get; private set; }
        public DataRequestQuery CurrentQuery { get; private set; }
    }
}