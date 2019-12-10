using System;
using System.Net;

namespace IO.Ably.MessageEncoders
{
    /// <summary>
    /// Specific error class that is used to distinguish a critical vcdiff error
    /// and a normal decoding error which could be caused by a bad encoding string or
    /// a bad cipher.
    /// </summary>
    internal class VcdiffErrorInfo : ErrorInfo
    {
        public VcdiffErrorInfo(string reason)
            : base(reason, ErrorCodes.VCDiffDecodeError)
        {
        }

        public VcdiffErrorInfo(string reason, Exception innerException)
            : base(reason, ErrorCodes.VCDiffDecodeError, HttpStatusCode.BadRequest, innerException)
        {
        }
    }
}
