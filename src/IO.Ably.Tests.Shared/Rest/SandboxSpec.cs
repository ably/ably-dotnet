using System;
using System.Collections.Generic;
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

        private List<IRealtimeClient> RealtimeClients = new List<IRealtimeClient>();


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

        protected async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null, string environment = null)
        {
            var settings = await Fixture.GetSettings(environment);
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

            // Prevent the Xunit concurrent context being captured which is
            // an implementation of <see cref="SynchronizationContext"/> which runs work on custom threads
            // rather than in the thread pool, and limits the number of in-flight actions.
            //
            // This can create out of order responses that would not normally occur
            defaultOptions.CaptureCurrentSynchronizationContext = false;
            optionsAction?.Invoke(defaultOptions, settings);
            var client = new AblyRealtime(defaultOptions, createRestFunc);

            RealtimeClients.Add(client);
            return client;
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
                try {
                    _output.WriteLine($"{level}: {message}");
                } catch (Exception ex) { 
                    //In rare events this happens and crashes the test runner
                    Console.WriteLine($"{level}: {message}");
                }
                
            }
        }

        protected Task WaitToBecomeConnected(AblyRealtime realtime, TimeSpan? waitSpan = null)
        {
            return WaitForState(realtime, waitSpan: waitSpan);
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
            Output.WriteLine("Closing connections: " + RealtimeClients.Count);
            foreach (var client in RealtimeClients)
            {
                try
                {

                    client.Close();
                }
                catch (Exception ex)
                {
                    Output?.WriteLine("Error disposing Client: " + ex.Message);
                }
            }
            ResetEvent?.Dispose();
        }
    }
}
