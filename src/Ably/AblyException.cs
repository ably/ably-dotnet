using System;
using System.Net;
using System.Runtime.Serialization;

namespace Ably
{

    /// <summary>
    /// Ably exception wrapper class. It includes error information <see cref="Ably.ErrorInfo"/> used by ably. 
    /// All inner exceptions are wrapped in this class. Always check the inner exception property of the caught exception.
    /// </summary>
#if NET
    [Serializable]
#endif
    public class AblyException : Exception
    {
        public AblyException()
        {

        }

        /// <summary>
        /// Creates AblyException
        /// </summary>
        /// <param name="reason">Reason passed to the error info class</param>
        public AblyException(string reason)
            : this(new ErrorInfo(reason, 500, null))
        {

        }

        /// <summary>
        /// Creates AblyException. ErrorInfo is automatically generated based on the inner exception message. StatusCode is set to 50000
        /// </summary>
        /// <param name="ex">Original exception to be wrapped.</param>
        public AblyException(Exception ex)
            : this(new ErrorInfo("Unexpected error :" + ex.Message, 50000), ex)
            {
                
            }

        /// <summary>
        /// Creates AblyException and populates ErrorInfo with the supplied parameters.
        /// </summary>
        public AblyException(string reason, int code, HttpStatusCode? statusCode = null) : this(new ErrorInfo(reason, code, statusCode))
        {
            
        }

        /// <summary>
        /// Creates AblyException with supplied error info.
        /// </summary>
        public AblyException(ErrorInfo info)
            : base(info.ToString())
        {
            ErrorInfo = info;
        }

        /// <summary>
        /// Creates AblyException with ErrorInfo and sets the supplied exception as innerException
        /// </summary>
        public AblyException(ErrorInfo info, Exception innerException)
            : base(info.ToString(), innerException)
        {
            ErrorInfo = info;
        }

#if NET
        protected AblyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
#endif

        public ErrorInfo ErrorInfo { get; set; }

        internal static AblyException FromResponse(AblyResponse response)
        {
            return new AblyException(ErrorInfo.Parse(response));
        }
    }
}