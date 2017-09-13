using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Shared;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class AblyRealtimeSpecs : MockHttpRestSpecs
    {
        AutoResetEvent Signal = new AutoResetEvent(false);

        public void WaitOne()
        {
            var result = Signal.WaitOne(2000);
            Assert.True(result, "Result was not returned withing 2000ms");
        }

        public void Done()
        {
            Signal.Set();
        }

        internal virtual AblyRealtime GetRealtimeClient(ClientOptions options = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);
            clientOptions.SkipInternetCheck = true; //This is for the Unit tests
            clientOptions.UseSyncForTesting = true;
            clientOptions.CaptureCurrentSynchronizationContext = false;
            return new AblyRealtime(clientOptions, opts => GetRestClient(handleRequestFunc, clientOptions));
        }

        internal virtual AblyRealtime GetRealtimeClient(Action<ClientOptions> optionsAction, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var options = new ClientOptions(ValidKey);
            options.SkipInternetCheck = true; //This is for the Unit tests
            options.UseSyncForTesting = true;
            options.CaptureCurrentSynchronizationContext = false;
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

        public DateTimeOffset Now
        {
            get => NowProvider.Now();
            set { throw new NotImplementedException(); }
        }

        public INowProvider NowProvider { get; set; }

        public AblySpecs(ITestOutputHelper output)
        {
            NowProvider = new AblySpecsNowProvider();
            Output = output;
        }

        internal class AblySpecsNowProvider : INowProvider
        {
            public DateTimeOffset Now()
            {
                return TestHelpers.Now();
            }
        }
    }
}