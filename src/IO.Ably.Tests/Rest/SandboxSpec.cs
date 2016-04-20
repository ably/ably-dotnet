using System;
using System.Threading.Tasks;
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
            //Logger.LoggerSink = new OutputLoggerSink(output);
        }

        protected async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Protocol.MsgPack;
            optionsAction?.Invoke(defaultOptions);
            return new AblyRest(defaultOptions);
        }

        class OutputLoggerSink : ILoggerSink
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
    }
}