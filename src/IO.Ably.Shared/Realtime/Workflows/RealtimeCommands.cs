using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Realtime.Workflow
{
    internal class PingCommand : RealtimeCommand
    {
        internal PingRequest Request { get; }

        public PingCommand(PingRequest request)
        {
            Request = request;
        }

        protected override string ExplainData()
        {
            return "Ping request id: " + Request.Id;
        }
    }

    internal class InitCommand : RealtimeCommand
    {
        public static InitCommand Create() => new InitCommand();

        protected override string ExplainData()
        {
            return DateTimeOffset.UtcNow.ToString();
        }
    }

    internal class PingTimerCommand : RealtimeCommand
    {
        public string PingRequestId { get; }

        protected override string ExplainData()
        {
            return "PingRequest id: " + PingRequestId;
        }

        public PingTimerCommand(string pingRequestId)
        {
            PingRequestId = pingRequestId;
        }

        public static PingTimerCommand Create(string pingRequestId) => new PingTimerCommand(pingRequestId);
    }

    internal class ListCommand : RealtimeCommand
    {
        public IEnumerable<RealtimeCommand> Commands { get; }

        public ListCommand(IEnumerable<RealtimeCommand> commands)
        {
            Commands = commands.ToList();
        }

        public static ListCommand Create(params RealtimeCommand[] commands) => new ListCommand(commands);

        protected override string ExplainData()
        {
            return "Commands: " + Commands.Select(x => x.Name).JoinStrings();
        }
    }

    internal class EmptyCommand : RealtimeCommand
    {
        public static EmptyCommand Instance = new EmptyCommand();

        private EmptyCommand()
        {
        }

        protected override string ExplainData()
        {
            return string.Empty;
        }
    }

    internal class ProcessMessageCommand : RealtimeCommand
    {
        public ProcessMessageCommand(ProtocolMessage protocolMessage)
        {
            ProtocolMessage = protocolMessage;
        }

        public ProtocolMessage ProtocolMessage { get; }

        public static ProcessMessageCommand Create(ProtocolMessage message) => new ProcessMessageCommand(message);

        protected override string ExplainData()
        {
            return "Message: " + ProtocolMessage.ToJson();
        }
    }

    internal class ConnectCommand : RealtimeCommand
    {
        public static ConnectCommand Create() => new ConnectCommand();

        protected override string ExplainData() => string.Empty;
    }

    internal class SetInitStateCommand : RealtimeCommand
    {
        public string Recover { get; set; }

        public SetInitStateCommand(string recover)
        {
            Recover = recover;
        }

        public static SetInitStateCommand Create(string recover) => new SetInitStateCommand(recover);

        protected override string ExplainData()
        {
            return string.Empty;
        }
    }

    internal class CloseConnectionCommand : RealtimeCommand
    {
        public static CloseConnectionCommand Create() => new CloseConnectionCommand();

        protected override string ExplainData() => string.Empty;
    }

    internal class SetConnectingStateCommand : RealtimeCommand
    {
        public bool ClearConnectionKey { get; set; } = false;

        public SetConnectingStateCommand(bool clearConnectionKey)
        {
            ClearConnectionKey = clearConnectionKey;
        }

        public static SetConnectingStateCommand Create(bool clearConnectionKey = false) => new SetConnectingStateCommand(clearConnectionKey);

        protected override string ExplainData()
        {
            return string.Empty;
        }
    }

    internal class SetConnectedStateCommand : RealtimeCommand
    {
        public ProtocolMessage Message { get; }

        public bool IsUpdate { get; }

        public SetConnectedStateCommand(ProtocolMessage message, bool isUpdate)
        {
            Message = message;
            IsUpdate = isUpdate;
        }

        public static SetConnectedStateCommand Create(ProtocolMessage message, bool isUpdate) =>
                new SetConnectedStateCommand(message, isUpdate);

        protected override string ExplainData()
        {
            return $"IsUpdate: {IsUpdate}. Message: {Message.ToJson()}";
        }
    }

    internal class SetDisconnectedStateCommand : RealtimeCommand
    {
        public SetDisconnectedStateCommand(ErrorInfo error, bool retryInstantly, bool skipAttach, Exception exception, bool clearConnectionKey)
        {
            Error = error;
            RetryInstantly = retryInstantly;
            SkipAttach = skipAttach;
            Exception = exception;
            ClearConnectionKey = clearConnectionKey;
        }

        public ErrorInfo Error { get; }

        public bool RetryInstantly { get; }

        public bool SkipAttach { get; }

        public Exception Exception { get; }

        public bool ClearConnectionKey { get; }

        protected override string ExplainData()
        {
            return $"RetryInstantly: {RetryInstantly}" +
                   "SkipAttach: " + SkipAttach +
                   ((Error != null) ? " Error: " + Error : string.Empty) +
                    ((Exception != null) ? " Exception: " + Exception.Message : string.Empty) +
                " ClearConnectionKey: " + ClearConnectionKey;
        }

        public static SetDisconnectedStateCommand Create (
            ErrorInfo error,
            bool retryInstantly = false,
            bool skipAttach = false,
            Exception exception = null,
            bool clearConnectionKey = false)
            => new SetDisconnectedStateCommand(error, retryInstantly, skipAttach, exception, clearConnectionKey);
    }

    internal class SetSuspendedStateCommand : RealtimeCommand
    {
        public SetSuspendedStateCommand(ErrorInfo error, bool clearConnectionKey)
        {
            Error = error;
            ClearConnectionKey = clearConnectionKey;
        }

        public ErrorInfo Error { get; }

        public bool ClearConnectionKey { get; }

        public static SetSuspendedStateCommand Create(ErrorInfo error, bool clearConnectionKey = false) => new SetSuspendedStateCommand(error, clearConnectionKey);

        protected override string ExplainData()
        {
            var message = (Error != null) ? " Error: " + Error : string.Empty;
            message += " ClearConnectionKey:" + ClearConnectionKey;
            return message;
        }
    }

    internal class SetFailedStateCommand : RealtimeCommand
    {
        public SetFailedStateCommand(ErrorInfo error)
        {
            Error = error;
        }

        public ErrorInfo Error { get; }

        protected override string ExplainData()
        {
            return (Error != null) ? "Error: " + Error.ToString() : string.Empty;
        }

        public static SetFailedStateCommand Create(ErrorInfo error) => new SetFailedStateCommand(error);
    }

    internal class SetClosingStateCommand : RealtimeCommand
    {
        protected override string ExplainData()
        {
            return string.Empty;
        }

        public static SetClosingStateCommand Create() => new SetClosingStateCommand();
    }

    internal class SetClosedStateCommand : RealtimeCommand
    {
        public ErrorInfo Error { get; }

        public Exception Exception { get; }

        public SetClosedStateCommand(ErrorInfo error, Exception exception = null)
        {
            Exception = exception;
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public static SetClosedStateCommand Create(ErrorInfo error = null, Exception exception = null) =>
            new SetClosedStateCommand(error, exception);

        protected override string ExplainData()
        {
            return $"Error: " + Error.ToString();
        }
    }

    internal class RetryAuthCommand : RealtimeCommand
    {
        public ErrorInfo Error { get; }

        public bool UpdateState { get; }

        public RetryAuthCommand(ErrorInfo error, bool updateState)
        {
            Error = error;
            UpdateState = updateState;
        }

        public static RetryAuthCommand Create(ErrorInfo error, bool updateState) => new RetryAuthCommand(error, updateState);

        public static RetryAuthCommand Create(bool updateState) => new RetryAuthCommand(null, updateState);

        protected override string ExplainData() => "UpdatedState: " + UpdateState + ((Error != null) ? " " + Error : string.Empty);
    }

    internal class SendMessageCommand : RealtimeCommand
    {
        public ProtocolMessage ProtocolMessage { get; }

        public Action<bool, ErrorInfo> Callback { get; }

        public bool Force { get; }

        public SendMessageCommand(ProtocolMessage protocolMessage, Action<bool, ErrorInfo> callback, bool force)
        {
            ProtocolMessage = protocolMessage;
            Callback = callback;
            Force = force;
        }

        public static SendMessageCommand Create(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, bool force = false) => new SendMessageCommand(message, callback, force);

        protected override string ExplainData()
        {
            return "Protocol message: " + ProtocolMessage.ToJson();
        }
    }

    internal class DelayCommand : RealtimeCommand
    {
        public TimeSpan Delay { get; }

        public RealtimeCommand CommandToQueue { get; }

        public DelayCommand(TimeSpan delay, RealtimeCommand commandToQueue)
        {
            Delay = delay;
            CommandToQueue = commandToQueue;
        }

        public static DelayCommand Create(TimeSpan delay, RealtimeCommand command) => new DelayCommand(delay, command);

        protected override string ExplainData()
        {
            return $"Delay: {Delay.ToString()}. Command: {CommandToQueue.Name}";
        }
    }

    internal class HandleConnectingTokenErrorCommand : RealtimeCommand
    {
        public ErrorInfo Error { get; }

        public HandleConnectingTokenErrorCommand(ErrorInfo error)
        {
            Error = error;
        }

        public static HandleConnectingTokenErrorCommand Create(ErrorInfo error) => new HandleConnectingTokenErrorCommand(error);

        protected override string ExplainData()
        {
            return "Error: " + Error.ToString();
        }
    }

    internal class HandleConnectingFailureCommand : RealtimeCommand
    {
        public ErrorInfo Error { get; }

        public Exception Exception { get; }

        public bool ClearConnectionKey { get; }

        public HandleConnectingFailureCommand(ErrorInfo error, Exception ex, bool clearConnectionKey)
        {
            Error = error;
            Exception = ex;
            ClearConnectionKey = clearConnectionKey;
        }

        public static HandleConnectingFailureCommand Create(ErrorInfo error = null, Exception ex = null, bool clearConnectionKey = false) =>
            new HandleConnectingFailureCommand(error, ex, clearConnectionKey);

        protected override string ExplainData()
        {
            return $"Error: {Error}. Exception: {Exception?.Message}. ClearConnectionKey: {ClearConnectionKey}";
        }
    }

    internal class HandleTrasportEventCommand : RealtimeCommand
    {
        public TransportState TransportState { get; }

        public Exception Exception { get; }

        public HandleTrasportEventCommand(TransportState transportState, Exception ex)
        {
            TransportState = transportState;
            Exception = ex;
        }

        public static HandleTrasportEventCommand Create(TransportState state, Exception ex) =>
            new HandleTrasportEventCommand(state, ex);

        protected override string ExplainData()
        {
            return $"TrasportState: {TransportState}. Exception: {Exception?.Message}";
        }
    }
}