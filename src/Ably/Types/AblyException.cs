using System;

namespace Ably.Types
{
    public class AblyException : Exception
    {
        public AblyException(string reason, int statusCode, int code)
            : base(reason)
        {
            this.ErrorInfo = new ErrorInfo(reason, statusCode, code);
        }

        public ErrorInfo ErrorInfo { get; private set; }
    }
}
