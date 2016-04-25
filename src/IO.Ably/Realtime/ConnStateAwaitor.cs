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

        public readonly Connection connection;
        private readonly ConnectionState m_awaitedState;
        private readonly TaskCompletionSource<Connection> m_tcs = new TaskCompletionSource<Connection>();

        public ConnStateAwaitor(Connection connection, ConnectionState awaitedState = ConnectionState.Connected)
        {
            this.connection = connection;
            m_awaitedState = awaitedState;
            connection.ConnectionStateChanged += conn_StateChanged;
        }

        public void Dispose()
        {
            connection.ConnectionStateChanged -= conn_StateChanged;
        }

        private void conn_StateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.CurrentState == m_awaitedState)
            {
                m_tcs.SetResult(connection);
                return; // Success :-)
            }

            if (!s_hsPermanentlyFailedStates.Contains(e.CurrentState))
                return; // Still trying :-/

            // Failed :-(
            if (null != e.Reason)
                m_tcs.SetException(e.Reason.AsException());

            // TODO: improve error message
            m_tcs.SetException(new Exception("Connection is in some failed state " + e.CurrentState));
        }

        public Task<Connection> wait()
        {
            return wait(TimeSpan.FromMinutes(1));
        }

        public async Task<Connection> wait(TimeSpan timeout)
        {
            var tResult = m_tcs.Task;
            var tTimeout = Task.Delay(timeout);
            var tCompleted = await Task.WhenAny(tResult, tTimeout);
            if (tCompleted == tResult)
                return tResult.Result;
            m_tcs.SetException(new TimeoutException());
            return await tResult;
        }
    }
}