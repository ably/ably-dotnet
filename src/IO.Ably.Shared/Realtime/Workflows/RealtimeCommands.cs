using System;
using System.Collections.Generic;
using System.Linq;
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
        private EmptyCommand() {}


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

    internal class CloseConnectionCommand : RealtimeCommand
    {
        public static CloseConnectionCommand Create() => new CloseConnectionCommand();

        protected override string ExplainData() => String.Empty;
    }

    internal class SetConnectingStateCommand : RealtimeCommand
    {
        public static SetConnectingStateCommand Create() => new SetConnectingStateCommand();

        protected override string ExplainData()
        {
            return String.Empty;
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
        public SetDisconnectedStateCommand(ErrorInfo error, bool retryInstantly, bool skipAttach, Exception exception)
        {
            Error = error;
            RetryInstantly = retryInstantly;
            SkipAttach = skipAttach;
            Exception = exception;
        }

        public ErrorInfo Error { get; }

        public bool RetryInstantly { get; }

        public bool SkipAttach { get; }
        public Exception Exception { get; }


        protected override string ExplainData()
        {
            return $"RetryInstantly: {RetryInstantly}" +
                   "SkipAttach: " + SkipAttach +
                   ((Error != null) ? " Error: " + Error : string.Empty) +
                    ((Exception != null) ? " Exception: " + Exception.Message : string.Empty);
        }

        public static SetDisconnectedStateCommand Create (
            ErrorInfo error,
            bool retryInstantly = false,
            bool skipAttach = false,
            Exception exception = null)
            => new SetDisconnectedStateCommand(error, retryInstantly, skipAttach, exception);
    }

    internal class SetSuspendedStateCommand : RealtimeCommand
    {
        public SetSuspendedStateCommand(ErrorInfo error)
        {
            Error = error;
        }

        public ErrorInfo Error { get; }

        public static SetSuspendedStateCommand Create(ErrorInfo error) => new SetSuspendedStateCommand(error);

        protected override string ExplainData()
        {
            return (Error != null) ? " Error: " + Error : string.Empty;
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

        protected override string ExplainData() => "UpdatedState: " + UpdateState + ((Error != null) ? " " + Error.ToString() : "");
    }

    internal class SendMessageCommand : RealtimeCommand
    {
        public ProtocolMessage ProtocolMessage { get; }
        public Action<bool, ErrorInfo> Callback { get; }

        public SendMessageCommand(ProtocolMessage protocolMessage, Action<bool, ErrorInfo> callback)
        {
            ProtocolMessage = protocolMessage;
            Callback = callback;
        }

        public static SendMessageCommand Create(ProtocolMessage message, Action<bool, ErrorInfo> callback = null) => new SendMessageCommand(message, callback);

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
}