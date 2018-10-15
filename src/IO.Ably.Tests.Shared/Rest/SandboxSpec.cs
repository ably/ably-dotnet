using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class SandboxSpecs : IClassFixture<AblySandboxFixture>, IDisposable
    {
        internal ILogger Logger { get; set; }

        protected AblySandboxFixture Fixture { get; }

        protected ITestOutputHelper Output { get; }

        protected ManualResetEvent ResetEvent { get; }

        public SandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
        {
            ResetEvent = new ManualResetEvent(false);
            Fixture = fixture;
            Output = output;
            Logger = DefaultLogger.LoggerInstance;

            // Reset time in case other tests have changed it
            // Config.Now = () => DateTimeOffset.UtcNow;

            // Very useful for debugging failing tests.
            // Logger.LoggerSink = new OutputLoggerSink(output);
            // Logger.LogLevel = LogLevel.Debug;
        }

        protected async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Defaults.Protocol;
            optionsAction?.Invoke(defaultOptions);
            return new AblyRest(defaultOptions);
        }

        protected async Task<AblyRealtime> GetRealtimeClient(
            Protocol protocol,
            Action<ClientOptions, TestEnvironmentSettings> optionsAction = null)
        {
            return await GetRealtimeClient(protocol, optionsAction, null);
        }

        protected async Task<AblyRealtime> GetRealtimeClient(
            Protocol protocol,
            Action<ClientOptions, TestEnvironmentSettings> optionsAction,
            Func<ClientOptions, AblyRest> createRestFunc)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Defaults.Protocol;
            defaultOptions.TransportFactory = new TestTransportFactory();

            // Prevent the Xunit concurrent context being caputured which is
            // an implementation of <see cref="SynchronizationContext"/> which runs work on custom threads
            // rather than in the thread pool, and limits the number of in-flight actions.
            //
            // This can create out of order responses that would not normally occur
            defaultOptions.CaptureCurrentSynchronizationContext = false;
            optionsAction?.Invoke(defaultOptions, settings);
            return new AblyRealtime(defaultOptions, createRestFunc);
        }

        protected async Task WaitFor(Action<Action> done)
        {
            await TestHelpers.WaitFor(10000, 1, done);
        }

        protected async Task WaitFor(int timeoutMs, Action<Action> done)
        {
            await TestHelpers.WaitFor(timeoutMs, 1, done);
        }

        protected async Task WaitFor(int timeoutMs, int taskCount, Action<Action> done)
        {
            await TestHelpers.WaitFor(timeoutMs, taskCount, done);
        }

        protected async Task WaitForMultiple(int taskCount, Action<Action> done)
        {
            await TestHelpers.WaitFor(10000, taskCount, done);
        }

        public class OutputLoggerSink : ILoggerSink
        {
            private readonly ITestOutputHelper _output;

            public OutputLoggerSink(ITestOutputHelper output)
            {
                _output = output;
            }

            public void LogEvent(LogLevel level, string message)
            {
                _output.WriteLine($"{level}: {message}");
            }
        }

        protected Task WaitForState(AblyRealtime realtime, ConnectionState awaitedState = ConnectionState.Connected, TimeSpan? waitSpan = null)
        {
            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
            {
                return connectionAwaiter.Wait(waitSpan.Value);
            }

            return connectionAwaiter.Wait();
        }

        protected static async Task WaitFor30SOrUntilTrue(Func<bool> predicate)
        {
            int count = 0;
            while (count < 30)
            {
                count++;

                if (predicate())
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }

        public void Dispose()
        {
            ResetEvent?.Dispose();
        }
    }

    public static class SandboxSpecExtension
    {
        internal static Task WaitForState(this AblyRealtime realtime, ConnectionState awaitedState = ConnectionState.Connected, TimeSpan? waitSpan = null)
        {
            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
            {
                return connectionAwaiter.Wait(waitSpan.Value);
            }

            return connectionAwaiter.Wait();
        }

        internal static Task WaitForState(this IRealtimeChannel channel, ChannelState awaitedState = ChannelState.Attached, TimeSpan? waitSpan = null)
        {
            var channelAwaiter = new ChannelAwaiter(channel, awaitedState);
            if (waitSpan.HasValue)
            {
                return channelAwaiter.WaitAsync();
            }

            return channelAwaiter.WaitAsync();
        }
    }
}
