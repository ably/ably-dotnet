using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport.States.Connection;

namespace IO.Ably.Transport
{
    internal class ConnectionAttemptsInfo
    {
        internal Func<DateTimeOffset> Now { get; set; }

        public ConnectionAttemptsInfo(Func<DateTimeOffset> now = null)
        {
            Now = now ?? Defaults.NowFunc();
        }

        internal List<ConnectionAttempt> Attempts { get; } = new List<ConnectionAttempt>();

        internal DateTimeOffset? FirstAttempt => Attempts.Any() ? Attempts.First().Time : (DateTimeOffset?)null;

        internal int NumberOfAttempts => Attempts.Count;

        internal bool TriedToRenewToken { get; private set; }

        public void Reset()
        {
            Attempts.Clear();
            TriedToRenewToken = false;
        }

        public void RecordAttemptFailure(ConnectionState state, ErrorInfo error)
        {
            var attempt = Attempts.LastOrDefault() ?? new ConnectionAttempt(Now());
            attempt.FailedStates.Add(new AttemptFailedState(state, error));
            if (Attempts.Count == 0)
            {
                Attempts.Add(attempt);
            }
        }

        public void RecordAttemptFailure(ConnectionState state, Exception ex)
        {
            if (Attempts.Any())
            {
                var attempt = Attempts.Last();
                attempt.FailedStates.Add(new AttemptFailedState(state, ex));
            }
        }

        public void RecordTokenRetry()
        {
            TriedToRenewToken = true;
        }

        public int DisconnectedCount() => Attempts.SelectMany(x => x.FailedStates)
            .Count(x => x.State == ConnectionState.Disconnected && x.ShouldUseFallback());

        public int SuspendedCount() => Attempts.SelectMany(x => x.FailedStates)
            .Count(x => x.State == ConnectionState.Suspended);

        public void UpdateAttemptState(ConnectionStateBase newState)
        {
            switch (newState.State)
            {
                case ConnectionState.Connecting:
                    Attempts.Add(new ConnectionAttempt(Now()));
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
