using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    /// <summary>Utility class to wait for a specified state of the connection, with timeout.</summary>
    internal class ConnStateAwaitor : IDisposable
    {
        private static readonly HashSet<ConnectionState> s_hsPermanentlyFailedStates = new HashSet<ConnectionState>
        {
            ConnectionState.Suspended,
            ConnectionState.Closed,
            ConnectionState.Failed
        };

        private readonly ConnectionState _awaitedState;

        public readonly Connection Connection;
        private readonly TaskCompletionSource<Connection> taskCompletionSource = new TaskCompletionSource<Connection>();

        public ConnStateAwaitor(Connection connection, ConnectionState awaitedState = ConnectionState.Connected)
        {
            Connection = connection;
            _awaitedState = awaitedState;
            connection.ConnectionStateChanged += conn_StateChanged;
        }

        public void Dispose()
        {
            Connection.ConnectionStateChanged -= conn_StateChanged;
        }

        private void conn_StateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.CurrentState == _awaitedState)
            {
                taskCompletionSource.SetResult(Connection);
                return; // Success :-)
            }

            if (!s_hsPermanentlyFailedStates.Contains(e.CurrentState))
                return; // Still trying :-/

            // Failed :-(
            if (null != e.Reason)
                taskCompletionSource.SetException(e.Reason.AsException());

            // TODO: improve error message
            taskCompletionSource.SetException(new Exception("Connection is in some failed state " + e.CurrentState));
        }

        public Task<Connection> wait()
        {
            return wait(TimeSpan.FromMinutes(1));
        }

        public async Task<Connection> wait(TimeSpan timeout)
        {
            var tResult = taskCompletionSource.Task;
            var tTimeout = Task.Delay(timeout);
            var tCompleted = await Task.WhenAny(tResult, tTimeout);
            if (tCompleted == tResult)
                return tResult.Result;
            taskCompletionSource.SetException(new TimeoutException());
            return await tResult;
        }
    }
}