using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public abstract class SandboxSpecs
    {
        protected readonly AblySandboxFixture Fixture;
        protected readonly ITestOutputHelper Output;

        public SandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
            //Reset time in case other tests have changed it
            Config.Now = () => DateTimeOffset.UtcNow;

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

        protected Task WaitForState(AblyRealtime realtime, ConnectionStateType awaitedState = ConnectionStateType.Connected, TimeSpan? waitSpan = null)
        {
            var connectionAwaiter = new ConnectionAwaiter(realtime.Connection, awaitedState);
            if (waitSpan.HasValue)
                return connectionAwaiter.Wait(waitSpan.Value);
            return connectionAwaiter.Wait();
        }
    }
}