﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Push;
using IO.Ably.Realtime;
using UnityEngine.TestTools;

namespace Assets.Tests.AblySandbox
{
    public class AblySandbox: IDisposable
    {
        private readonly List<AblyRealtime> _realtimeClients = new List<AblyRealtime>();

        public AblySandbox(AblySandboxFixture fixture)
        {
            ResetEvent = new ManualResetEvent(false);
            Fixture = fixture;
            Logger = DefaultLogger.LoggerInstance;

            // Reset time in case other tests have changed it
            // Config.Now = () => DateTimeOffset.UtcNow;

            // Very useful for debugging failing tests.
            Logger.LoggerSink = new OutputLoggerSink();
            Logger.LogLevel = LogLevel.Warning;
        }

        ILogger Logger { get; set; }

        public AblySandboxFixture Fixture { get; set; }

        public ManualResetEvent ResetEvent { get; }

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
            Logger.Debug($"Test end disposing connections: {_realtimeClients.Count}");
            foreach (var client in _realtimeClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error disposing Client: {ex.Message}");
                }
            }

            ResetEvent?.Dispose();
        }

        public async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null, string environment = null)
        {
            var settings = await Fixture.GetSettings(environment);
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Defaults.Protocol;
            optionsAction?.Invoke(defaultOptions);
            return new AblyRest(defaultOptions);
        }

        public async Task<AblyRealtime> GetRealtimeClient(Protocol protocol, Action<ClientOptions, TestEnvironmentSettings> optionsAction = null, Func<ClientOptions, IMobileDevice, AblyRest> createRestFunc = null)
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

        public async Task AssertMultipleTimes(Func<Task> testAction, int maxNumberOfTimes, TimeSpan durationBetweenAttempts)
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

        private class OutputLoggerSink : ILoggerSink
        {
            public void LogEvent(LogLevel level, string message)
            {
                try
                {
                    message = $"{level}: {message}";
                    switch (level)
                    {
                        // Throws exception for error since LogAssert.Expect/ LogAssert.ignoreFailingMessages cannot be accessed and
                        // error log needs to be disabled
                        case LogLevel.Error:
                        case LogLevel.Warning:
                            UnityEngine.Debug.LogWarning(message);
                            break;
                        default:
                            UnityEngine.Debug.Log(message);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    message = $"{level}: {message}. Exception: {ex.Message}";
                    UnityEngine.Debug.LogWarning(message);
                }
            }
        }
    }
}
