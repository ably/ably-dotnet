using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably.Tests.Infrastructure
{
    /// <summary>Utility class to wait for a specified state of the connection, with timeout.</summary>
    internal class ConnStateAwaitor : IDisposable
    {
        private static readonly HashSet<ConnectionStateType> PermanentlyFailedStates = new HashSet<ConnectionStateType>
        {
            ConnectionStateType.Suspended,
            ConnectionStateType.Closed,
            ConnectionStateType.Failed
        };

        private readonly ConnectionStateType _awaitedState;

        public readonly Connection Connection;
        private readonly TaskCompletionSource<Connection> _taskCompletionSource = new TaskCompletionSource<Connection>();

        public ConnStateAwaitor(Connection connection, ConnectionStateType awaitedState = ConnectionStateType.Connected)
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
                _taskCompletionSource.SetResult(Connection);
                return; // Success :-)
            }

            if (!PermanentlyFailedStates.Contains(e.CurrentState))
                return; // Still trying :-/

            // Failed :-(
            if (null != e.Reason)
                _taskCompletionSource.SetException(e.Reason.AsException());

            // TODO: improve error message
            _taskCompletionSource.SetException(new Exception("Connection is in some failed state " + e.CurrentState));
        }

        public Task<Connection> Wait()
        {
            return Wait(TimeSpan.FromSeconds(1));
        }

        public async Task<Connection> Wait(TimeSpan timeout)
        {
            var tResult = _taskCompletionSource.Task;
            var tTimeout = Task.Delay(timeout);
            var tCompleted = await Task.WhenAny(tResult, tTimeout);
            if (tCompleted == tResult)
                return tResult.Result;
            _taskCompletionSource.SetException(new TimeoutException());
            return await tResult;
        }
    }
}
