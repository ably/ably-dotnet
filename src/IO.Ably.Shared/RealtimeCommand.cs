using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Types;

namespace IO.Ably
{
    internal abstract class RealtimeCommand
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

        public string Name => GetType().Name;

        public string Explain()
        {
            var data = ExplainData();
            if (data.IsNotEmpty())
            {
                data = " Data: " + data;
            }

            return $"{GetType().Name}:{data} Meta:{Id}|{Created:s}";
        }

        protected abstract string ExplainData();
    }

    internal class PingCommand : RealtimeCommand
    {
        internal PingRequest Request { get; }

        ctor
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

    internal class DisconnetCommand : RealtimeCommand
    {
        public static DisconnetCommand Create() => new DisconnetCommand();

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
}