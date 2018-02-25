using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably.Tests
{
    /// <summary>Utility class to wait for a specified state of the connection, with timeout.</summary>
    internal class ConnectionAwaiter
    {
        private static readonly HashSet<ConnectionState> PermanentlyFailedStates = new HashSet<ConnectionState>
        {
            ConnectionState.Suspended,
            ConnectionState.Closed,
            ConnectionState.Failed
        };

        private readonly List<ConnectionState> _awaitedStates = new List<ConnectionState>();

        public readonly Connection Connection;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private readonly string _id = Guid.NewGuid().ToString("D").Split('-')[0];

        public ConnectionAwaiter(Connection connection, params ConnectionState[] awaitedStates)
        {
            Connection = connection;
            if (awaitedStates != null && awaitedStates.Length > 0)
            {
                _awaitedStates.AddRange(awaitedStates);
            }
            else
            {
                throw new ArgumentNullException(nameof(awaitedStates), "Please add at least one awaited state");
            }
        }

        private void RemoveListener()
        {
            DefaultLogger.Debug($"[{_id}] Removing Connection listener");
            Connection.InternalStateChanged -= conn_StateChanged;
        }

        private void conn_StateChanged(object sender, ConnectionStateChange e)
        {
            if (_awaitedStates.Contains(e.Current))
            {
                DefaultLogger.Debug($"[{_id}] Desired state was reached.");
                RemoveListener();
                _taskCompletionSource.SetResult(true);
            }
        }

        public Task<TimeSpan> Wait()
        {
            return Wait(TimeSpan.FromSeconds(16));
        }

        public async Task<TimeSpan> Wait(TimeSpan timeout)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug($"[{_id}] Waiting for state {string.Join(",", _awaitedStates)} for {timeout.TotalSeconds} seconds");
            }

            if (_awaitedStates.Contains(Connection.State))
            {
                DefaultLogger.Debug($"Current state is {Connection.State}. Desired state reached.");
                return TimeSpan.Zero;
            }

            Connection.InternalStateChanged += conn_StateChanged;
            var tResult = _taskCompletionSource.Task;
            var tCompleted = await Task.WhenAny(tResult, Task.Delay(timeout)).ConfigureAwait(true);
            if (tCompleted == tResult)
            {
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }

            DefaultLogger.Debug($"[{_id} Timeout exceeded. Throwing TimeoutException");
            RemoveListener();
            throw new TimeoutException();
        }
    }
}
