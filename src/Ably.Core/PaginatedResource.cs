using System.Collections.Generic;
using System.Net;

namespace IO.Ably
{
    public class PaginatedResource<T> : List<T>
    {
        private readonly int _limit;

        public PaginatedResource() : this( null, Config.Limit )
        {}

        public PaginatedResource( WebHeaderCollection headers, int limit)
        {
            _limit = limit;
            if( null != headers )
            {
                CurrentQuery = DataRequestQuery.GetLinkQuery( headers, DataRequestLinkType.Current );
                NextQuery = DataRequestQuery.GetLinkQuery( headers, DataRequestLinkType.Next );
                FirstQuery = DataRequestQuery.GetLinkQuery( headers, DataRequestLinkType.First );
            }
        }

        public bool HasNext { get { return null != NextQuery && NextQuery.IsEmpty == false; } }
        public DataRequestQuery NextQuery { get; private set; }
        public DataRequestQuery FirstQuery { get; private set; }
        public DataRequestQuery CurrentQuery { get; private set; }
    }
}
