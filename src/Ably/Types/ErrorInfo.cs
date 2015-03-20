using System;

namespace Ably.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ErrorInfo()
        {
        }

        /// <summary>
        /// Construct an ErrorInfo from message and code.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        public ErrorInfo(string message, int code)
            : this(message, code, 0)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="statusCode"></param>
        public ErrorInfo(string message, int code, int statusCode)
        {
            this.Message = message;
            this.Code = code;
        }

        /// <summary>
        /// Ably error code (see ably-common/protocol/errors.json)
        /// </summary>
        public int Code { get; private set; }
        
        /// <summary>
        /// HTTP Status Code corresponding to this error, where applicable
        /// </summary>
        public int StatusCode { get; private set; }

        /// <summary>
        /// Additional message information, where available
        /// </summary>
        public string Message { get; private set; }
    }
}
