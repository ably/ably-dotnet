﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage response;
        private readonly Action _sendAsyncAction;
        public HttpRequestMessage LastRequest;
        public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

        public FakeHttpMessageHandler(HttpResponseMessage response, Action sendAsyncAction = null)
        {
            this.response = response;
            _sendAsyncAction = sendAsyncAction;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfRequests++;
            Requests.Add(request);
            LastRequest = request;
            var responseTask = Task.FromResult(response);
            _sendAsyncAction?.Invoke();
            return responseTask;
        }

        public int NumberOfRequests { get; set; }
    }
}