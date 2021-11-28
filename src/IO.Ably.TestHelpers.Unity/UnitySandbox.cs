﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Push;
using IO.Ably.Realtime;
using NUnit.Framework.Internal;

namespace IO.Ably.TestHelpers.Unity
{
    public abstract class UnitySandboxSpecs : AblySandboxFixture, IDisposable
    {
        private readonly List<AblyRealtime> _realtimeClients = new List<AblyRealtime>();

        protected UnitySandboxSpecs(AblySandboxFixture fixture)
        {
            ResetEvent = new ManualResetEvent(false);
            Fixture = fixture;
            Logger = DefaultLogger.LoggerInstance;

            // Reset time in case other tests have changed it
            // Config.Now = () => DateTimeOffset.UtcNow;

            // Very useful for debugging failing tests.
            // Logger.LoggerSink = new OutputLoggerSink(output);
            // Logger.LogLevel = LogLevel.Debug;
        }

        internal ILogger Logger { get; set; }

        protected AblySandboxFixture Fixture { get; }

        protected ManualResetEvent ResetEvent { get; }

        public IDisposable EnableDebugLogging()
        {
            Logger.LoggerSink = new OutputLoggerSink();
            Logger.LogLevel = LogLevel.Debug;

            return new ActionOnDispose(() =>
            {
                Logger.LoggerSink = new DefaultLoggerSink();
                Logger.LogLevel = LogLevel.Warning;
            });
        }

        public void Dispose()
        {
            Logger.Debug("Test end disposing connections: " + _realtimeClients.Count);
            foreach (var client in _realtimeClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error disposing Client: " + ex.Message);
                }
            }

            ResetEvent?.Dispose();
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
            Func<ClientOptions, IMobileDevice, AblyRest> createRestFunc)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Defaults.Protocol;
            defaultOptions.TransportFactory = new TestTransportFactory();

            optionsAction?.Invoke(defaultOptions, settings);
            var client = new AblyRealtime(defaultOptions, createRestFunc);

            _realtimeClients.Add(client);
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

        protected async Task WaitForMultiple(int taskCount, Action<Action> done, Action onFail = null)
        {
            await TestHelpers.WaitFor(20000, taskCount, done, onFail);
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

        public class OutputLoggerSink : ILoggerSink
        {
            public void LogEvent(LogLevel level, string message)
            {
                try
                {
                    Debug.WriteLine($"{level}: {message}");
                    Console.WriteLine($"{level}: {message}");
                }
                catch (Exception ex)
                {
                    // In rare events this happens and crashes the test runner
                    Console.WriteLine($"{level}: {message}. Exception: {ex.Message}");
                }
            }
        }
    }
}
