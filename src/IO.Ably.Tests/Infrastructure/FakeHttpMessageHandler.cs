using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage response;
        public HttpRequestMessage LastRequest;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var responseTask = new TaskCompletionSource<HttpResponseMessage>();
            responseTask.SetResult(response);

            return responseTask.Task;
        }
    }
}