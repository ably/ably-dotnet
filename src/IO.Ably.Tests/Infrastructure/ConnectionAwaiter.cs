using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.Realtime;

namespace IO.Ably.Tests
{
    /// <summary>Utility class to wait for a specified state of the connection, with timeout.</summary>
    internal class ConnectionAwaiter
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
        private readonly string _id = Guid.NewGuid().ToString("D").Split('-')[0];

        public ConnectionAwaiter(Connection connection, ConnectionStateType awaitedState = ConnectionStateType.Connected)
        {
            Connection = connection;
            _awaitedState = awaitedState;
            Connection.ConnectionStateChanged += conn_StateChanged;
        }
        
        private void RemoveListener()
        {
            Logger.Debug($"[{_id}] Removing Connection listener");
            Connection.ConnectionStateChanged -= conn_StateChanged;
        }

        private void conn_StateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Logger.Debug($"[{_id}] Awaiter on state changed to " + e.CurrentState);
            if (e.CurrentState == _awaitedState)
            {
                Logger.Debug($"[{_id}] Desired state was reached.");
                _taskCompletionSource.SetResult(Connection);
                RemoveListener();
                return; // Success :-)
            }

            if (!PermanentlyFailedStates.Contains(e.CurrentState))
                return; // Still trying :-/

            RemoveListener();
            // Failed :-(
            if (null != e.Reason)
                _taskCompletionSource.SetException(e.Reason.AsException());

            _taskCompletionSource.SetException(new Exception("Connection is in some failed state " + e.CurrentState));
        }

        public async Task<Connection> Wait()
        {
            return await Wait(TimeSpan.FromSeconds(2));
        }

        public async Task<Connection> Wait(TimeSpan timeout)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug($"[{_id}] Waiting for state {_awaitedState} for {timeout.TotalSeconds} seconds");
            }

            if (Connection.State == _awaitedState)
                return Connection;

            var tResult = _taskCompletionSource.Task;
            var tCompleted = await Task.WhenAny(tResult, Task.Delay(timeout));
            if (tCompleted == tResult)
                return tResult.Result;
            
            Logger.Debug($"[{_id} Timeout exceeded. Throwing TimeoutException");
            RemoveListener();
            _taskCompletionSource.SetException(new TimeoutException());
            return await tResult;
        }
    }
}
