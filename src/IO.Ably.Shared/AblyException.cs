using System;
using System.Net;

namespace IO.Ably
{
    public class InsecureRequestException : AblyException
    {
        public InsecureRequestException()
            : base("Current action cannot be performed over http")
        {
        }
    }

    /// <summary>
    /// Ably exception wrapper class. It includes error information (<see cref="Ably.ErrorInfo"/>) used by Ably.
    /// All inner exceptions are wrapped in this class.
    /// Always check the inner exception property of the caught exception.
    /// </summary>
    public class AblyException : Exception
    {
        /// <inheritdoc />
        public AblyException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyException"/> class.
        /// Creates AblyException
        /// </summary>
        /// <param name="reason">Reason passed to the error info class</param>
        /// <param name="ex">Optional, the original exception to be wrapped.</param>
        public AblyException(string reason, Exception ex = null)
            : this(new ErrorInfo(reason, 50000, null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyException"/> class.
        /// Creates AblyException. ErrorInfo is automatically generated based on the inner exception message. StatusCode is set to 50000
        /// </summary>
        /// <param name="ex">Original exception to be wrapped.</param>
        public AblyException(Exception ex)
            : this(new ErrorInfo("Unexpected error :" + ex.Message, 50000), ex)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyException"/> class.
        /// </summary>
        /// <param name="reason">A brief, helpful message explaining the reason for the error</param>
        /// <param name="code">The Ably code (see spec item T11)</param>
        /// <param name="statusCode">Optional, analogous to HTTP status code</param>
        public AblyException(string reason, int code, HttpStatusCode? statusCode = null)
            : this(new ErrorInfo(reason, code, statusCode))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyException"/> class with <see cref="ErrorInfo"/> set.
        /// Configures ErrorInfo using the supplied parameters.
        /// </summary>
        /// <param name="reason">A brief, helpful message explaining the reason for the error</param>
        /// <param name="code">The Ably code (see spec item T11)</param>
        /// <param name="statusCode">Optional, analogous to HTTP status code</param>
        /// <param name="innerException">The underlying exception</param>
        public AblyException(string reason, int code, HttpStatusCode? statusCode, Exception innerException)
            : this(new ErrorInfo(reason, code, statusCode), innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyException"/> class.
        /// Creates AblyException with supplied <see cref="ErrorInfo"/>.
        /// </summary>
        /// <param name="info">An instance of <see cref="ErrorInfo"/></param>
        public AblyException(ErrorInfo info)
            : base(info.ToString())
        {
            ErrorInfo = info;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AblyException"/> class with <see cref="ErrorInfo"/> and sets the supplied exception as innerException
        /// </summary>
        /// <param name="info">An instance of <see cref="ErrorInfo"/></param>
        /// <param name="innerException">The underlying exception</param>
        public AblyException(ErrorInfo info, Exception innerException)
            : base(info.ToString(), innerException)
        {
            ErrorInfo = info;
        }

        /// <summary>
        /// Get and set <see cref="ErrorInfo"/>
        /// </summary>
        public ErrorInfo ErrorInfo { get; set; }

        internal static AblyException FromResponse(AblyResponse response)
        {
            return new AblyException(ErrorInfo.Parse(response));
        }
    }
}
