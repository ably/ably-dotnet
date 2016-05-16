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
            return error?.statusCode != null &&
                FallbackReasons.Contains(error.statusCode.Value) &&
                await RestClient.CanConnectToAbly();
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
                if (Attempts.Any())
                {
                    var attempt = Attempts.Last();
                    attempt.FailedStates.Add(new AttemptFailedState(state, error));
                }
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

        public void Increment()
        {
            lock (_syncLock)
            {
                Attempts.Add(new ConnectionAttempt(Config.Now()));
            }
        }

        private static string GetHost(ClientOptions options, bool useFallbackHost)
        {
            var defaultHost = Defaults.RealtimeHost;
            if (useFallbackHost)
            {
                var r = new Random();
                defaultHost = Defaults.FallbackHosts[r.Next(0, 1000) % Defaults.FallbackHosts.Length];
            }
            var host = options.RealtimeHost.IsNotEmpty() ? options.RealtimeHost : defaultHost;
            if (options.Environment.HasValue && options.Environment != AblyEnvironment.Live)
            {
                return string.Format("{0}-{1}", options.Environment.ToString().ToLower(), host);
            }
            return host;
        }

        public void UpdateAttemptState(ConnectionState newState)
        {
            lock (_syncLock)
                switch (newState.State)
                {
                    case ConnectionStateType.Connected:
                        Attempts.Clear();
                        break;
                    case ConnectionStateType.Failed:
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