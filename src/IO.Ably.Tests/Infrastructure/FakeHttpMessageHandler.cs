using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage response;
        public HttpRequestMessage LastRequest;
        public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfRequests++;
            Requests.Add(request);
            LastRequest = request;
            var responseTask = new TaskCompletionSource<HttpResponseMessage>();
            responseTask.SetResult(response);

            return responseTask.Task;
        }

        public int NumberOfRequests { get; set; }
    }
}