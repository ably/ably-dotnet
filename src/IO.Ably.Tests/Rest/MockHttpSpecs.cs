using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class MockHttpSpecs : AblySpecs
    {
        internal virtual AblyResponse DefaultResponse { get; }
        internal AblyRequest LastRequest { get; set; }
        internal AblyRest GetRestClient(Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null, Action<ClientOptions> setOptionsAction = null)
        {
            var options = new ClientOptions(ValidKey) { UseBinaryProtocol = false};
            setOptionsAction?.Invoke(options);

            var client = new AblyRest(options);
            client.ExecuteHttpRequest = request =>
            {
                LastRequest = request;
                if (handleRequestFunc != null)
                {
                    return handleRequestFunc(request);
                }
                return (DefaultResponse ?? AblyResponse.EmptyResponse).ToTask();
            };
            return client;
        }

        public MockHttpSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}