using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Realtime;
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
        public ConnectionStateType State { get; private set; }

        public AttemptFailedState(ConnectionStateType state, ErrorInfo error)
        {
            State = state;
            Error = error;
        }

        public AttemptFailedState(ConnectionStateType state, Exception ex)
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
            return State == ConnectionStateType.Disconnected || State == ConnectionStateType.Suspended;
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

        public ConnectionAttemptsInfo(Connection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            _connection = connection;
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

        public void RecordAttemptFailure(ConnectionStateType state, ErrorInfo error)
        {
            lock (_syncLock)
            {
                var attempt = Attempts.LastOrDefault() ?? new ConnectionAttempt(Config.Now());
                attempt.FailedStates.Add(new AttemptFailedState(state, error));
                if(Attempts.Count == 0)
                    Attempts.Add(attempt);
            }
        }

        public void RecordAttemptFailure(ConnectionStateType state, Exception ex)
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
                return (Config.Now() - FirstAttempt.Value) >= _connection.ConnectionStateTtl;
            }
        }

        public int DisconnectedCount => Attempts.SelectMany(x => x.FailedStates).Count(x => x.State == ConnectionStateType.Disconnected && x.ShouldUseFallback());
        public int SuspendedCount => Attempts.SelectMany(x => x.FailedStates).Count(x => x.State == ConnectionStateType.Suspended && x.ShouldUseFallback());

        public string GetHost()
        {
            var lastFailedState = Attempts.SelectMany(x => x.FailedStates).LastOrDefault(x => x.ShouldUseFallback());
            string customHost = "";
            if (lastFailedState != null)
            {
                if (lastFailedState.State == ConnectionStateType.Disconnected)
                {
                    customHost = _connection.FallbackHosts[DisconnectedCount%_connection.FallbackHosts.Count];
                }
                if (lastFailedState.State == ConnectionStateType.Suspended && SuspendedCount > 1)
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

        public void UpdateAttemptState(ConnectionState newState)
        {
            lock (_syncLock)
                switch (newState.State)
                {
                    case ConnectionStateType.Connecting:
                        Attempts.Add(new ConnectionAttempt(Config.Now()));
                        break;
                    case ConnectionStateType.Failed:
                    case ConnectionStateType.Closed:
                    case ConnectionStateType.Connected:
                        Reset();
                        break;
                    case ConnectionStateType.Suspended:
                    case ConnectionStateType.Disconnected:
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