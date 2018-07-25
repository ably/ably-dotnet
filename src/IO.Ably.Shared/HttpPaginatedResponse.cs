using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IO.Ably
{
    public class HttpPaginatedResponse : PaginatedResult<JToken>
    {
        private const string AblyErrorCodeHeader = "X-Ably-Errorcode";

        private const string AblyErrorMessageHeader = "X-Ably-Errormessage";

        public HttpHeaders Headers { get; private set; }

        public HttpStatusCode StatusCode { get; private set; }

        public bool Success => (int)StatusCode >= 200 && (int)StatusCode < 300;

        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        protected new Func<PaginatedRequestParams, Task<HttpPaginatedResponse>> ExecuteDataQueryFunc { get; }

        public HttpPaginatedResponse()
        {
        }

        internal HttpPaginatedResponse(AblyResponse response, int limit, PaginatedRequestParams requestParams, Func<PaginatedRequestParams, Task<HttpPaginatedResponse>> executeDataQueryFunc)
            : base(response, limit, null)
        {
            ExecuteDataQueryFunc = executeDataQueryFunc;
            StatusCode = Response.StatusCode;
            Headers = response.Headers;

            if (Response.Headers.TryGetValues(AblyErrorCodeHeader, out var errorCodeHeaderValues))
            {
                string errCodeStr = errorCodeHeaderValues.FirstOrDefault();
                if (int.TryParse(errCodeStr, out var errCode))
                {
                    ErrorCode = errCode;
                }
            }

            if (Response.Headers.TryGetValues(AblyErrorCodeHeader, out var errorMessageHeaderValues))
            {
                ErrorMessage = errorMessageHeaderValues.FirstOrDefault();
            }

            ExecuteDataQueryFunc = executeDataQueryFunc;

            var data = JToken.Parse(response.TextResponse);
            if (data is JArray arr)
            {
                foreach (var token in arr)
                {
                    Items.Add(token);
                }
            }
            else
            {
                Items.Add(data);
            }

            InitializeQuery(CurrentQuery, requestParams);
            InitializeQuery(NextDataQuery, requestParams);
        }

        private void InitializeQuery(PaginatedRequestParams queryParams, PaginatedRequestParams requestParams)
        {
            queryParams.Path = requestParams.Path;
            queryParams.HttpMethod = requestParams.HttpMethod;
            queryParams.Body = requestParams.Body;
            queryParams.Headers = requestParams.Headers;
        }

        public new Task<HttpPaginatedResponse> NextAsync()
        {
            if (HasNext && ExecuteDataQueryFunc != null)
            {
                return ExecuteDataQueryFunc(NextDataQuery);
            }

            return Task.FromResult(new HttpPaginatedResponse());
        }

        public new Task<HttpPaginatedResponse> FirstAsync()
        {
            if (FirstDataQuery != null && FirstDataQuery.IsEmpty == false && ExecuteDataQueryFunc != null)
            {
                return ExecuteDataQueryFunc(FirstDataQuery);
            }

            return Task.FromResult(new HttpPaginatedResponse());
        }

        public new HttpPaginatedResponse Next()
        {
            return AsyncHelper.RunSync(NextAsync);
        }

        public new HttpPaginatedResponse First()
        {
            return AsyncHelper.RunSync(FirstAsync);
        }
    }
}
