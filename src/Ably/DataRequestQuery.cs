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
    }
}
