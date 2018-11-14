using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace IO.Ably
{
    public class PaginatedResult<T>
        where T : class
    {
        internal AblyResponse Response { get; set; }

        protected int Limit { get; set; }

        protected Func<PaginatedRequestParams, Task<PaginatedResult<T>>> ExecuteDataQueryFunc { get; }

        public List<T> Items { get; set; } = new List<T>();

        public PaginatedRequestParams NextQueryParams { get; protected set; }

        public PaginatedRequestParams FirstQueryParams { get; protected set; }

        public PaginatedRequestParams CurrentQueryParams { get; protected set; }

        protected PaginatedResult()
        {
        }

        internal PaginatedResult(AblyResponse response, int limit, Func<PaginatedRequestParams, Task<PaginatedResult<T>>> executeDataQueryFunc)
        {
            Response = response;
            Limit = limit;
            ExecuteDataQueryFunc = executeDataQueryFunc;
            if (response.Headers != null)
            {
                CurrentQueryParams = PaginatedRequestParams.GetLinkQuery(response.Headers, DataRequestLinkType.Current);
                NextQueryParams = PaginatedRequestParams.GetLinkQuery(response.Headers, DataRequestLinkType.Next);
                FirstQueryParams = PaginatedRequestParams.GetLinkQuery(response.Headers, DataRequestLinkType.First);
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are further pages
        /// </summary>
        public bool HasNext => NextQueryParams != null && NextQueryParams.IsEmpty == false;

        /// <summary>
        /// Gets a value indicating whether the current page is the last one available
        /// </summary>
        public bool IsLast => HasNext == false;

        public Task<PaginatedResult<T>> NextAsync()
        {
            if (HasNext && ExecuteDataQueryFunc != null)
            {
                return ExecuteDataQueryFunc(NextQueryParams);
            }

            return Task.FromResult(new PaginatedResult<T>());
        }

        public Task<PaginatedResult<T>> FirstAsync()
        {
            if (FirstQueryParams != null && FirstQueryParams.IsEmpty == false && ExecuteDataQueryFunc != null)
            {
                return ExecuteDataQueryFunc(FirstQueryParams);
            }

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
    }
}
