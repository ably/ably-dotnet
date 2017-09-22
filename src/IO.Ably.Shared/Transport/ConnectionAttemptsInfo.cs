using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably;
using IO.Ably.Transport.States.Connection;

namespace IO.Ably.Transport
{
    internal sealed class ConnectionAttempt
    {
        public DateTimeOffset Time { get; }
        public List<AttemptFailedState> FailedStates { get; private set; } = new List<AttemptFailedState>();

        public ConnectionAttempt(DateTimeOffset time)
        {
            Time = time;
        }
    }

    internal sealed class AttemptFailedState
    {
        public ErrorInfo Error { get; private set; }
        public Exception Exception { get; private set; }
        public ConnectionState State { get; private set; }

        public AttemptFailedState(ConnectionState state, ErrorInfo error)
        {
            State = state;
            Error = error;
        }

        public AttemptFailedState(ConnectionState state, Exception ex)
        {
            State = state;
            Exception = ex;
        }

        public bool ShouldUseFallback()
        {
            return IsFailedOrSuspendedState() &&
                (IsRecoverableError() || IsRecoverableException());
        }

        private bool IsFailedOrSuspendedState()
        {
            return State == ConnectionState.Disconnected || State == ConnectionState.Suspended;
        }

        private bool IsRecoverableException()
        {
            return Exception != null;
        }

        private bool IsRecoverableError()
        {
            return (Error != null && Error.IsRetryableStatusCode());
        }
    }

    internal sealed class ConnectionAttemptsInfo
    {
        internal INowProvider NowProvider { get; set; }
        private static readonly ISet<HttpStatusCode> FallbackReasons;

        static ConnectionAttemptsInfo()
        {
            FallbackReasons = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.GatewayTimeout
            };
        }

        internal List<ConnectionAttempt> Attempts { get; } = new List<ConnectionAttempt>();
        internal DateTimeOffset? FirstAttempt => Attempts.Any() ? Attempts.First().Time : (DateTimeOffset?)null;
        public AblyRest RestClient => _connection.RestClient;
        public ClientOptions Options => RestClient.Options;
        private readonly Connection _connection;
        internal int NumberOfAttempts => Attempts.Count;
        internal bool TriedToRenewToken { get; private set; }

        private readonly object _syncLock = new object();

        public ConnectionAttemptsInfo(Connection connection, INowProvider nowProvider)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            NowProvider = nowProvider;
        }

        public ConnectionAttemptsInfo(Connection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            NowProvider = connection.NowProvider;
        }

        public async Task<bool> CanFallback(ErrorInfo error)
        {
            return IsDefaultHost() &&
                error != null && error.IsRetryableStatusCode() &&
                await RestClient.CanConnectToAbly();
        }

        private bool IsDefaultHost()
        {
            return Options.IsDefaultRealtimeHost;
        }

        public void Reset()
        {
            lock (_syncLock)
            {
                Attempts.Clear();
                TriedToRenewToken = false;
            }
        }

        public void RecordAttemptFailure(ConnectionState state, ErrorInfo error)
        {
            lock (_syncLock)
            {
                var attempt = Attempts.LastOrDefault() ?? new ConnectionAttempt(NowProvider.Now());
                attempt.FailedStates.Add(new AttemptFailedState(state, error));
                if(Attempts.Count == 0)
                    Attempts.Add(attempt); 
            }
        }

        public void RecordAttemptFailure(ConnectionState state, Exception ex)
        {
            lock (_syncLock)
            {
                if (Attempts.Any())
                {
                    var attempt = Attempts.Last();
                    attempt.FailedStates.Add(new AttemptFailedState(state, ex));
                }
            }
        }

        public void RecordTokenRetry()
        {
            lock (_syncLock)
                TriedToRenewToken = true;
        }

        public bool ShouldSuspend()
        {
            lock (_syncLock)
            {
                if (FirstAttempt == null)
                    return false;
                return (NowProvider.Now() - FirstAttempt.Value) >= _connection.ConnectionStateTtl;
            }
        }

        public int DisconnectedCount => Attempts.SelectMany(x => x.FailedStates).Count(x => x.State == ConnectionState.Disconnected && x.ShouldUseFallback());
        public int SuspendedCount => Attempts.SelectMany(x => x.FailedStates).Count(x => x.State == ConnectionState.Suspended && x.ShouldUseFallback());

        public string GetHost()
        {
            var lastFailedState = Attempts.SelectMany(x => x.FailedStates).LastOrDefault(x => x.ShouldUseFallback());
            string customHost = "";
            if (lastFailedState != null)
            {
                if (lastFailedState.State == ConnectionState.Disconnected)
                {
                    customHost = _connection.FallbackHosts[DisconnectedCount%_connection.FallbackHosts.Count];
                }
                if (lastFailedState.State == ConnectionState.Suspended && SuspendedCount > 1)
                {
                    customHost =
                        _connection.FallbackHosts[(DisconnectedCount + SuspendedCount)%_connection.FallbackHosts.Count];
                }

                if (customHost.IsNotEmpty())
                {
                    _connection.Host = customHost;
                    return customHost;
                }
            }

            _connection.Host = Options.FullRealtimeHost();
            return _connection.Host;
        }

        public void UpdateAttemptState(ConnectionStateBase newState)
        {
            lock (_syncLock)
                switch (newState.State)
                {
                    case ConnectionState.Connecting:
                        Attempts.Add(new ConnectionAttempt(NowProvider.Now()));
                        break;
                    case ConnectionState.Failed:
                    case ConnectionState.Closed:
                    case ConnectionState.Connected:
                        Reset();
                        break;
                    case ConnectionState.Suspended:
                    case ConnectionState.Disconnected:
                        if (newState.Exception != null)
                        {
                            RecordAttemptFailure(newState.State, newState.Exception);
                        }
                        else
                        {
                            RecordAttemptFailure(newState.State, newState.Error);
                        }
                        break;
                }
        }
    }
}