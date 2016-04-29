using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class AblyRealtimeSpecs : MockHttpRestSpecs
    {
        internal virtual AblyRealtime GetRealtimeClient(ClientOptions options = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);
            clientOptions.SkipInternetCheck = true; //This is for the Unit tests
            return new AblyRealtime(clientOptions, opts => GetRestClient(handleRequestFunc, clientOptions));
        }

        internal virtual AblyRealtime GetRealtimeClient(Action<ClientOptions> optionsAction, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var options = new ClientOptions(ValidKey);
            options.SkipInternetCheck = true; //This is for the Unit tests
            optionsAction?.Invoke(options);
            return new AblyRealtime(options, clientOptions => GetRestClient(handleRequestFunc, clientOptions));
        }

        public AblyRealtimeSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }

    public abstract class AblySpecs
    {
        public ITestOutputHelper Output { get; }
        public const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        public DateTimeOffset Now { get; set; }

        public AblySpecs(ITestOutputHelper output)
        {
            Now = Config.Now();
            Config.Now = () => Now;    
            Output = output;
        }
    }
}