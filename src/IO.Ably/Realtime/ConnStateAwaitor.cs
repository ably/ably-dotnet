using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Realtime
{
    /// <summary>Utility class to wait for a specified state of the connection, with timeout.</summary>
    internal class ConnStateAwaitor : IDisposable
    {
        static readonly HashSet<ConnectionState> s_hsPermanentlyFailedStates = new HashSet<ConnectionState>()
        {
            ConnectionState.Suspended,
            ConnectionState.Closed,
            ConnectionState.Failed,
        };

        public ConnStateAwaitor( Connection connection, ConnectionState awaitedState = ConnectionState.Connected )
        {
            this.connection = connection;
            m_awaitedState = awaitedState;
            connection.ConnectionStateChanged += conn_StateChanged;
        }

        public void Dispose()
        {
            this.connection.ConnectionStateChanged -= conn_StateChanged;
        }

        void conn_StateChanged( object sender, ConnectionStateChangedEventArgs e )
        {
            if( e.CurrentState == m_awaitedState )
            {
                m_tcs.SetResult( this.connection );
                return; // Success :-)
            }

            if( !s_hsPermanentlyFailedStates.Contains( e.CurrentState ) )
                return; // Still trying :-/

            // Failed :-(
            if( null != e.Reason )
                m_tcs.SetException( e.Reason.AsException() );

            // TODO: improve error message
            m_tcs.SetException( new Exception( "Connection is in some failed state " + e.CurrentState.ToString() ) );
        }

        public readonly Connection connection;
        readonly ConnectionState m_awaitedState;
        readonly TaskCompletionSource<Connection> m_tcs = new TaskCompletionSource<Connection>();

        public Task<Connection> wait()
        {
            return wait( TimeSpan.FromMinutes( 1 ) );
        }

        public async Task<Connection> wait( TimeSpan timeout )
        {
            Task<Connection> tResult = m_tcs.Task;
            Task tTimeout = Task.Delay( timeout );
            Task tCompleted = await Task.WhenAny( tResult, tTimeout );
            if( tCompleted == tResult )
                return tResult.Result;
            m_tcs.SetException( new TimeoutException() );
            return await tResult;
        }
    }
}