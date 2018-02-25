using System;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    internal class FakeHttpClient : IAblyHttpClient
    {
        public Func<AblyRequest, AblyResponse> ExecuteFunc = delegate { return new AblyResponse(); };

        public Task<AblyResponse> Execute(AblyRequest request)
        {
            return Task.FromResult(ExecuteFunc(request));
        }
    }
}