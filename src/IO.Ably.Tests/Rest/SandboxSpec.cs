using System;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class SandboxSpecs
    {
        internal ILogger Logger { get; set; }

        protected readonly AblySandboxFixture Fixture;
        protected readonly ITestOutputHelper Output;
        protected ManualResetEvent _resetEvent;

        public SandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
        {
            _resetEvent = new ManualResetEvent(false);
            Fixture = fixture;
            Output = output;
            Logger = IO.Ably.DefaultLogger.LoggerInstance;
            //Reset time in case other tests have changed it
            //Config.Now = () => DateTimeOffset.UtcNow;

            // Very useful for debugging failing tests.
            //Logger.LoggerSink = new OutputLoggerSink(output);
            //Logger.LogLevel = LogLevel.Debug;
        }

        protected async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Protocol.MsgPack;
            optionsAction?.Invoke(defaultOptions);
            return new AblyRest(defaultOptions);
        }

        protected async Task<AblyRealtime> GetRealtimeClient(Protocol protocol,
            Action<ClientOptions, TestEnvironmentSettings> optionsAction = null)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Protocol.MsgPack;
            defaultOptions.TransportFactory = new TestTransportFactory();
            optionsAction?.Invoke(defaultOptions, settings);
            return new AblyRealtime(defaultOptions);
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
                return connectionAwaiter.Wait(waitSpan.Value);
            return connectionAwaiter.Wait();
        }

        protected static async Task WaitFor30sOrUntilTrue(Func<bool> predicate)
        {
            int count = 0;
            while (count < 30)
            {
                count++;

                if (predicate())
                    break;

                await Task.Delay(1000);
            }
        }
    }
}