using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private List<AblyRealtime> RealtimeClients = new List<AblyRealtime>();

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

            optionsAction?.Invoke(defaultOptions, settings);
            var client = new AblyRealtime(defaultOptions, createRestFunc);

            RealtimeClients.Add(client);
            return client;
        }

        protected async Task WaitFor(Action<Action> done)
        {
            await TestHelpers.WaitFor(10000, 1, done);
        }

        protected async Task AssertMultipleTimes(
            Func<Task> testAction,
            int maxNumberOfTimes,
            TimeSpan durationBetweenAttempts)
        {
            for (int i = 0; i < maxNumberOfTimes; i++)
            {
                try
                {
                    await testAction();
                    break; // If there were no exceptions then we are all good and can return
                }
                catch (Exception)
                {
                    await Task.Delay(durationBetweenAttempts);
                }
            }
        }

        protected async Task WaitFor(int timeoutMs, Action<Action> done, Action onFail = null)
        {
            await TestHelpers.WaitFor(timeoutMs, 1, done, onFail);
        }

        protected async Task WaitFor(int timeoutMs, int taskCount, Action<Action> done, Action onFail = null)
        {
            await TestHelpers.WaitFor(timeoutMs, taskCount, done);
        }

        protected async Task WaitForMultiple(int taskCount, Action<Action> done, Action onFail = null)
        {
            await TestHelpers.WaitFor(10000, taskCount, done, onFail);
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
                try
                {
                    Debug.WriteLine($"{level}: {message}");
                    _output.WriteLine($"{level}: {message}");
                }
                catch (Exception ex)
                {
                    // In rare events this happens and crashes the test runner
                    Console.WriteLine($"{level}: {message}. Exception: {ex.Message}");
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
            Output.WriteLine("Test end disposing connections: " + RealtimeClients.Count);
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

            ResetEvent?.Dispose();
        }
    }
}
