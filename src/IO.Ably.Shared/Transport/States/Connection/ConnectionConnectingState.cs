﻿using System;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public ConnectionConnectingState(IConnectionContext context, ILogger logger, bool isUpdate = false)
            : this(context, new CountdownTimer("Connecting state timer", logger), logger, isUpdate)
        {
        }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer, ILogger logger, bool isUpdate = false)
            : base(context, logger)
        {
            _timer = timer;
            IsUpdate = isUpdate;
        }

        public override ConnectionState State => ConnectionState.Connecting;

        public override bool CanQueue => true;

        public override RealtimeCommand Connect()
        {
            return EmptyCommand.Instance;
        }

        public override void Close()
        {
            TransitionState(SetClosingStateCommand.Create().TriggeredBy("ConnectingState.Close()"));
        }

        public override async Task<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "Null message passed to Connection Connecting State");
            }

            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Connected:
                    {
                        if (Context.Transport.State == TransportState.Connected)
                        {
                            TransitionState(SetConnectedStateCommand.Create(message, false)
                                .TriggeredBy("ConnectingState.OnMessageReceived(Connected)"));
                        }

                        return true;
                    }

                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        Context.ExecuteCommand(HandleConnectingDisconnectedCommand.Create(message.Error)
                            .TriggeredBy("ConnectingState.OnMessageReceived(Disconnected)"));
                        return true;
                    }

                case ProtocolMessage.MessageAction.Error:
                    {
                        Context.ExecuteCommand(HandleConnectingErrorCommand.Create(message.Error)
                            .TriggeredBy("ConnectingState.OnMessageReceived(Error)"));
                        return true;
                    }
            }

            return false;
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override void StartTimer()
        {
            _timer.Start(Context.DefaultTimeout, onTimeOut: OnTimeOut);
        }

        private void OnTimeOut()
        {
            Context.ExecuteCommand(HandleConnectingDisconnectedCommand.Create(new ErrorInfo("Connecting timeout", ErrorCodes.ConnectionTimedOut, HttpStatusCode.GatewayTimeout)).TriggeredBy("ConnectingState.OnTimeOut()"));
        }

        private void TransitionState(RealtimeCommand command)
        {
            _timer.Abort();
            Context.ExecuteCommand(command);
        }
    }
}
