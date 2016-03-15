using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

namespace Ably
{
    public interface IPaginatedResource<out T> : IEnumerable<T>
    {
        bool HasNext { get; }
        DataRequestQuery NextQuery { get; }
        DataRequestQuery FirstQuery { get; }
        DataRequestQuery CurrentQuery { get; }
    }

    internal interface IPaginatedResource
    {
        DataRequestQuery NextQuery { get; set; }
        DataRequestQuery FirstQuery { get; set;  }
        DataRequestQuery CurrentQuery { get; set;  }
    }



    public class PaginatedResource<T> : List<T>, IPaginatedResource<T>, IPaginatedResource
    {
        private readonly int _limit;

        public PaginatedResource() : this(Config.Limit)
        {

        }

        public PaginatedResource(int limit)
        {
            _limit = limit;
        }

        public bool HasNext { get { return NextQuery.IsEmpty == false; } }
        public DataRequestQuery NextQuery { get; set; }
        public DataRequestQuery FirstQuery { get; set; }
        public DataRequestQuery CurrentQuery { get; set; }
    }

    public class PaginatedResource
    {
        public static PaginatedResource<T> InitializePartialResult<T>( WebHeaderCollection headers, int? limit = null)
        {
            var result = new PaginatedResource<T>(limit ?? Config.Limit);
            result.CurrentQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Current);
            result.NextQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.Next);
            result.FirstQuery = DataRequestQuery.GetLinkQuery(headers, DataRequestLinkType.First);
            return result;
        }
    }
}