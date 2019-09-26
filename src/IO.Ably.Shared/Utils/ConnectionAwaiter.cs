using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably.Utils
{
    internal class ConnectionChangeAwaiter
    {
        private ConnectionState _currentState;

        private readonly Connection _connection;
        private readonly string _id = Guid.NewGuid().ToString("D").Split('-')[0];

        public ConnectionChangeAwaiter(Connection connection)
        {
            _connection = connection;
            _currentState = _connection.State;
        }

        public Task<(bool, ConnectionState?)> Wait()
        {
            return Wait(TimeSpan.FromSeconds(16));
        }

        public async Task<(bool, ConnectionState?)> Wait(TimeSpan timeout)
        {
            if (DefaultLogger.IsDebug)
            {
                DefaultLogger.Debug($"[{_id}] Waiting for state change for {timeout.TotalSeconds} seconds");
            }

            TaskCompletionSource<(bool, ConnectionState?)> taskCompletionSource =
                new TaskCompletionSource<(bool, ConnectionState?)>();

            try
            {
                _connection.On(ChangeListener);
                var tResult = taskCompletionSource.Task;
                return await tResult.TimeoutAfter(timeout, (false, null));
            }
            finally
            {
                _connection.Off(ChangeListener);
            }

            void ChangeListener(ConnectionStateChange change)
            {
                _currentState = change.Current;
                taskCompletionSource.SetResult((true, _currentState));
            }
        }
    }

    internal class ConnectionAwaiter
    {
        private readonly List<ConnectionState> _awaitedStates = new List<ConnectionState>();

        private readonly Connection _connection;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private readonly string _id = Guid.NewGuid().ToString("D").Split('-')[0];

        public ConnectionAwaiter(Connection connection, params ConnectionState[] awaitedStates)
        {
            _connection = connection;
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
            _connection.Off(Conn_StateChanged);
        }

        private void Conn_StateChanged(ConnectionStateChange e)
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
                DefaultLogger.Debug(
                    $"[{_id}] Waiting for state {string.Join(",", _awaitedStates)} for {timeout.TotalSeconds} seconds");
            }

            if (_awaitedStates.Contains(_connection.State))
            {
                DefaultLogger.Debug($"Current state is {_connection.State}. Desired state reached.");
                return TimeSpan.Zero;
            }

            _connection.On(Conn_StateChanged);
            var tResult = _taskCompletionSource.Task;
            var tCompleted = await Task.WhenAny(tResult, Task.Delay(timeout)).ConfigureAwait(false);
            if (tCompleted == tResult)
            {
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }

            DefaultLogger.Debug($"[{_id} Timeout exceeded. Throwing TimeoutException");
            RemoveListener();
            throw new TimeoutException(
                $"Expected ''{_awaitedStates.Select(x => x.ToString()).JoinStrings()}' but current state was '{_connection.State}'");
        }
    }
}