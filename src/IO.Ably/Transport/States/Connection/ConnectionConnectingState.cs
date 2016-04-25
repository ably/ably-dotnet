using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionState
    {
        private const int ConnectTimeout = 15*1000;
        private static readonly ISet<HttpStatusCode> FallbackReasons;

        private readonly ICountdownTimer _timer;
        private readonly bool _useFallbackHost;

        static ConnectionConnectingState()
        {
            FallbackReasons = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.GatewayTimeout
            };
        }

        public ConnectionConnectingState(IConnectionContext context, bool useFallbackHost = false) :
            this(context, new CountdownTimer(), useFallbackHost)
        {
        }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer, bool useFallbackHost = false)
            :
                base(context)
        {
            _timer = timer;
            _useFallbackHost = useFallbackHost;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Connecting;

        protected override bool CanQueueMessages => true;

        public override void Connect()
        {
            // do nothing
        }

        public override void Close()
        {
            TransitionState(new ConnectionClosingState(context));
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Connected:
                {
                    if (context.Transport.State == TransportState.Connected)
                    {
                        var info = new ConnectionInfo(message.connectionId, message.connectionSerial ?? -1,
                            message.connectionKey);
                        TransitionState(new ConnectionConnectedState(context, info));
                    }
                    return true;
                }
                case ProtocolMessage.MessageAction.Disconnected:
                {
                    ConnectionState nextState;
                    if (ShouldSuspend())
                    {
                        nextState = new ConnectionSuspendedState(context, message.error);
                    }
                    else
                    {
                        nextState = new ConnectionDisconnectedState(context, message.error)
                        {
                            UseFallbackHost = ShouldUseFallbackHost(message.error)
                        };
                    }
                    TransitionState(nextState);
                    return true;
                }
                case ProtocolMessage.MessageAction.Error:
                {
                    if (ShouldUseFallbackHost(message.error))
                    {
                        context.Connection.Key = null;
                        TransitionState(new ConnectionDisconnectedState(context) {UseFallbackHost = true});
                    }
                    TransitionState(new ConnectionFailedState(context, message.error));
                    return true;
                }
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.Error != null || state.State == TransportState.Closed)
            {
                ConnectionState nextState;
                if (ShouldSuspend())
                {
                    nextState = new ConnectionSuspendedState(context);
                }
                else
                {
                    nextState = new ConnectionDisconnectedState(context, state)
                    {
                        UseFallbackHost = state.Error != null && CanConnectToAbly()
                    };
                }
                TransitionState(nextState);
            }
        }

        public override void OnAttachedToContext()
        {
            context.AttemptConnection();

            if (context.Transport == null || _useFallbackHost)
            {
                context.CreateTransport(_useFallbackHost);
            }

            if (context.Transport.State != TransportState.Connected)
            {
                context.Transport.Connect();
                _timer.Start(ConnectTimeout, () =>
                {
                    context.SetState(new ConnectionDisconnectedState(context, ErrorInfo.ReasonTimeout)
                    {
                        UseFallbackHost = CanConnectToAbly()
                    });
                });
            }
        }

        private static bool ShouldUseFallbackHost(ErrorInfo error)
        {
            return error != null && error.statusCode != null && FallbackReasons.Contains(error.statusCode.Value) &&
                   CanConnectToAbly();
        }

        private void TransitionState(ConnectionState newState)
        {
            context.SetState(newState);
            _timer.Abort();
        }

        private bool ShouldSuspend()
        {
            return context.FirstConnectionAttempt != null &&
                   context.FirstConnectionAttempt.Value
                       .AddMilliseconds(ConnectionSuspendedState.SuspendTimeout) < DateTimeOffset.UtcNow;
        }

        public static bool CanConnectToAbly()
        {
            var req = WebRequest.Create(Defaults.InternetCheckURL);
            WebResponse res = null;
            try
            {
                Func<Task<WebResponse>> fn = () => req.GetResponseAsync();
                res = Task.Run(fn).Result;
            }
            catch (Exception)
            {
                return false;
            }
            using (var resStream = res.GetResponseStream())
            {
                using (var reader = new StreamReader(resStream))
                {
                    return reader.ReadLine() == Defaults.InternetCheckOKMessage;
                }
            }
        }
    }
}