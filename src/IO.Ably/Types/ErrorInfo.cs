using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>
    /// An exception type encapsulating error informaiton containing an Ably specific error code and generic status code
    /// </summary>
    public class ErrorInfo
    {
        internal static readonly ErrorInfo ReasonClosed = new ErrorInfo("Connection closed by client", 10000);
        internal static readonly ErrorInfo ReasonDisconnected = new ErrorInfo("Connection temporarily unavailable", 80003);
        internal static readonly ErrorInfo ReasonSuspended = new ErrorInfo("Connection unavailable", 80002);
        internal static readonly ErrorInfo ReasonFailed = new ErrorInfo("Connection failed", 80000);
        internal static readonly ErrorInfo ReasonRefused = new ErrorInfo("Access refused", 40100);
        internal static readonly ErrorInfo ReasonTooBig = new ErrorInfo("Connection closed; message too large", 40000);
        internal static readonly ErrorInfo ReasonNeverConnected = new ErrorInfo("Unable to establish connection", 80002);
        internal static readonly ErrorInfo ReasonTimeout = new ErrorInfo("Unable to establish connection", 80014);
        internal static readonly ErrorInfo ReasonUnknown = new ErrorInfo("Unknown error", 50000, System.Net.HttpStatusCode.InternalServerError);

        internal const string CodePropertyName = "code";
        internal const string StatusCodePropertyName = "statusCode";
        internal const string ReasonPropertyName = "message";

        /// <summary>Ably error code (see https://github.com/ably/ably-common/blob/master/protocol/errors.json) </summary>
        public int code { get; set; }
        /// <summary>The http status code corresponding to this error</summary>
        public HttpStatusCode? statusCode { get; set; }
        /// <summary>Additional reason information, where available</summary>
        public string message { get; set; }

        public bool IsUnAuthorizedError => statusCode.HasValue &&
                                           statusCode.Value == HttpStatusCode.Unauthorized;

        public bool IsTokenError => code >= Defaults.TokenErrorCodesRangeStart &&
                                    code <= Defaults.TokenErrorCodesRangeEnd;

        public ErrorInfo() { }

        public ErrorInfo(string reason, int code)
        {
            this.code = code;
            this.message = reason;
        }

        public ErrorInfo(string reason, int code, HttpStatusCode? statusCode = null)
        {
            this.code = code;
            this.statusCode = statusCode;
            this.message = reason;
        }

        public override string ToString()
        {
            if (statusCode.HasValue == false)
            {
                return string.Format("Reason: {0}; Code: {1}", message, code);
            }
            return string.Format("Reason: {0}; Code: {1}; HttpStatusCode: ({2}){3}", message, code, (int)statusCode.Value, statusCode);
        }

        internal static ErrorInfo Parse(AblyResponse response)
        {
            string reason = "";
            int errorCode = 500;

            if (response.Type == ResponseType.Json)
            {

                try
                {
                    var json = JObject.Parse(response.TextResponse);
                    if (json["error"] != null)
                    {
                        reason = (string)json["error"]["message"];
                        errorCode = (int)json["error"]["code"];
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine( ex.Message );
                    //If there is no json or there is something wrong we don't want to throw from here. The
                }
            }
            return new ErrorInfo(StringExtensions.IsEmpty(reason) ? "Unknown error" : reason, errorCode, response.StatusCode);
        }

        public Exception AsException()
        {
            // TODO: implement own exception class instead, to have both codes in the exception
            return new Exception( this.message );
        }
    }
}