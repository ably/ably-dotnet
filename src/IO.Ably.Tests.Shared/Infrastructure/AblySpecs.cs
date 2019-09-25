using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class AblyRealtimeSpecs : MockHttpRestSpecs, IDisposable
    {
        private AutoResetEvent _signal = new AutoResetEvent(false);

        public void WaitOne()
        {
            var result = _signal.WaitOne(2000);
            Assert.True(result, "Result was not returned within 2000ms");
        }

        public void Done()
        {
            _signal.Set();
        }

        /// <summary>
        /// Figure out a way to yield the current thread so a command can be processed
        /// </summary>
        /// <returns></returns>
        public async Task ProcessCommands(IRealtimeClient client)
        {
            //TODO: find a better way to do it
            await Task.Delay(100);
        }


        internal virtual AblyRealtime GetRealtimeClient(ClientOptions options = null, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var clientOptions = options ?? new ClientOptions(ValidKey);
            clientOptions.SkipInternetCheck = true; // This is for the Unit tests
            clientOptions.CaptureCurrentSynchronizationContext = false;
            var client = new AblyRealtime(clientOptions, opts => GetRestClient(handleRequestFunc, clientOptions));
            RealtimeClients.Add(client);
            return client;
        }

        internal virtual AblyRealtime GetRealtimeClient(Action<ClientOptions> optionsAction, Func<AblyRequest, Task<AblyResponse>> handleRequestFunc = null)
        {
            var options = new ClientOptions(ValidKey);
            options.SkipInternetCheck = true; // This is for the Unit tests
            options.CaptureCurrentSynchronizationContext = false;
            optionsAction?.Invoke(options);

            var client = new AblyRealtime(options, clientOptions => GetRestClient(handleRequestFunc, clientOptions));
            RealtimeClients.Add(client);
            return client;
        }

        public AblyRealtimeSpecs(ITestOutputHelper output)
            : base(output)
        {
        }

        public void Dispose()
        {
            foreach (var client in RealtimeClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Output?.WriteLine("Error disposing Client: " + ex.Message);
                }
            }

            _signal?.Dispose();
        }

        public List<AblyRealtime> RealtimeClients { get; set; } = new List<AblyRealtime>();
    }

    public abstract class AblySpecs
    {
        internal ILogger Logger { get; set; }

        public ITestOutputHelper Output { get; }

        public const string ValidKey = "1iZPfA.BjcI_g:wpNhw5RCw6rDjisl";

        public DateTimeOffset Now => NowFunc();

        public Func<DateTimeOffset> NowFunc { get; set; }

        public void SetNowFunc(Func<DateTimeOffset> nowFunc) => NowFunc = nowFunc;

        public void NowAddSeconds(int s)
        {
            NowAdd(TimeSpan.FromSeconds(s));
        }

        public void NowAdd(TimeSpan ts)
        {
            DateTimeOffset n = Now.Add(ts);
            SetNowFunc(() => n);
        }

        protected AblySpecs(ITestOutputHelper output)
        {
            Logger = DefaultLogger.LoggerInstance;
            NowFunc = TestHelpers.Now;
            Output = output;
        }
    }
}
