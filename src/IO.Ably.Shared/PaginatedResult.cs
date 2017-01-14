using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace IO.Ably
{
    public class PaginatedResult<T> where T : class
    {
        private readonly int _limit;
        private Func<HistoryRequestParams, Task<PaginatedResult<T>>> ExecuteDataQueryFunc { get; }
        public List<T> Items { get; set; } = new List<T>();

        private PaginatedResult()
        {
            
        }

        internal PaginatedResult(HttpHeaders headers, int limit, Func<HistoryRequestParams, Task<PaginatedResult<T>>> executeDataQueryFunc)
        {
            _limit = limit;
            ExecuteDataQueryFunc = executeDataQueryFunc;
            if (headers != null)
            {
                CurrentQuery = HistoryRequestParams.GetLinkQuery(headers, DataRequestLinkType.Current);
                NextDataQuery = HistoryRequestParams.GetLinkQuery(headers, DataRequestLinkType.Next);
                FirstDataQuery = HistoryRequestParams.GetLinkQuery(headers, DataRequestLinkType.First);
            }
        }

        public bool HasNext => NextDataQuery != null && NextDataQuery.IsEmpty == false;
        public bool IsLast => HasNext == false;

        public Task<PaginatedResult<T>> NextAsync()
        {
            if (HasNext && ExecuteDataQueryFunc != null)
                return ExecuteDataQueryFunc(NextDataQuery);

            return Task.FromResult(new PaginatedResult<T>());
        }

        public Task<PaginatedResult<T>> FirstAsync()
        {
            if (FirstDataQuery != null && FirstDataQuery.IsEmpty == false && ExecuteDataQueryFunc != null)
                return ExecuteDataQueryFunc(FirstDataQuery);

            return Task.FromResult(new PaginatedResult<T>());
        }

        public PaginatedResult<T> Next()
        {
            return AsyncHelper.RunSync(NextAsync);
        }

        public PaginatedResult<T> First()
        {
            return AsyncHelper.RunSync(FirstAsync);
        }

        public HistoryRequestParams NextDataQuery { get; }
        public HistoryRequestParams FirstDataQuery { get; private set; }
        public HistoryRequestParams CurrentQuery { get; private set; }
    }
}