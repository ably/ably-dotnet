using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        internal static readonly ErrorInfo ReasonUnknown = new ErrorInfo("Unknown error", 50000, HttpStatusCode.InternalServerError);

        internal const string CodePropertyName = "code";
        internal const string StatusCodePropertyName = "statusCode";
        internal const string ReasonPropertyName = "message";
        internal const string HrefBase = "https://help.ably.io/error/";

        /// <summary>Ably error code (see https://github.com/ably/ably-common/blob/master/protocol/errors.json) </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>The http status code corresponding to this error</summary>
        [JsonProperty("statusCode")]
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>Additional reason information, where available</summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Link to specification detail for this error code, where available. Spec TI4.
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }

        /// <summary>
        /// Is this Error as result of a 401 Unauthorized HTTP response
        /// </summary>
        public bool IsUnAuthorizedError => StatusCode.HasValue &&
                                           StatusCode.Value == HttpStatusCode.Unauthorized;

        /// <summary>
        /// Is this Error as result of a 403 Forbidden HTTP response
        /// </summary>
        public bool IsForbiddenError => StatusCode.HasValue &&
                                           StatusCode.Value == HttpStatusCode.Forbidden;

        public bool IsTokenError => Code >= Defaults.TokenErrorCodesRangeStart &&
                                    Code <= Defaults.TokenErrorCodesRangeEnd;

        /// <summary>
        /// Get or Sets the InnerException
        /// </summary>
        public Exception InnerException { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
        /// </summary>
        public ErrorInfo() { }

        public ErrorInfo(string reason)
            : this(reason, 0)
        {
        }

        public ErrorInfo(string reason, int code)
            : this(reason, code, null, null, null)
        {
        }

        public ErrorInfo(string reason, int code, HttpStatusCode? statusCode = null, string href = null, Exception innerException = null)
        {
            Code = code;
            StatusCode = statusCode;
            Message = reason;
            if (href.IsEmpty() && code > 0)
            {
                Href = GetHref(code);
            }
            else
            {
                Href = href;
            }

            InnerException = innerException;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder("[ErrorInfo ");
            result.Append("Reason: ").Append(LogMessage()).Append("; ");
            if (Code > 0)
            {
                result.Append("Code: ").Append(Code).Append("; ");
            }

            if (StatusCode != null)
            {
                result.Append("StatusCode: ").Append((int)StatusCode).Append(" (").Append(StatusCode.ToString()).Append(")").Append("; ");
            }

            if (Href != null)
            {
                result.Append("Href: ").Append(Href).Append(";");
            }

            result.Append(']');
            return result.ToString();
        }

        internal static ErrorInfo Parse(AblyResponse response)
        {
            // RSA4d, if we have 403 response default to code 40300, this may be overwritten
            // if the response has a usable JSON body
            int errorCode = response.StatusCode == HttpStatusCode.Forbidden ? 40300 : 50000;
            string reason = string.Empty;

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
                    // If there is no json or there is something wrong we don't want to throw from here.
                    Debug.WriteLine(ex.Message);
                }
            }

            return new ErrorInfo(reason.IsEmpty() ? "Unknown error" : reason, errorCode, response.StatusCode);
        }

        public Exception AsException()
        {
            return new AblyException(this);
        }

        public bool IsRetryableStatusCode()
        {
            return StatusCode.HasValue && IsRetryableStatusCode(StatusCode.Value);
        }

        private static string GetHref(int code)
        {
            return $"{HrefBase}{code}";
        }

        /// <summary>
        /// Spec: TI5
        /// </summary>
        private string LogMessage()
        {
            string errHref = null;
            var logMessage = Message ?? string.Empty;
            if (Href.IsNotEmpty())
            {
                errHref = Href;
            }
            else if (Code > 0)
            {
                errHref = GetHref(Code);
            }

            if (errHref != null && !logMessage.Contains(errHref))
            {
                logMessage += " (See " + errHref + ")";
            }

            return logMessage;
        }

        public static bool IsRetryableStatusCode(HttpStatusCode statusCode)
        {
            return statusCode >= (HttpStatusCode)500 && statusCode <= (HttpStatusCode)504;
        }
    }
}
