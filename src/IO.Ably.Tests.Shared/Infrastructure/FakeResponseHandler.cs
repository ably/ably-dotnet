using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    public class FakeResponseHandler : DelegatingHandler
    {
        private readonly Dictionary<Uri, HttpResponseMessage> _fakeResponses
            = new Dictionary<Uri, HttpResponseMessage>();

        public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage)
        {
            _fakeResponses.Add(uri, responseMessage);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_fakeResponses.ContainsKey(request.RequestUri))
            {
                return _fakeResponses[request.RequestUri];
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                { RequestMessage = request };
            }
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}