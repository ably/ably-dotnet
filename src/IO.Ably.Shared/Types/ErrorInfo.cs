using System;
using System.Diagnostics;
using System.Net;

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
        internal static readonly ErrorInfo ReasonUnknown = new ErrorInfo("Unknown error", 50000, System.Net.HttpStatusCode.InternalServerError);

        internal const string CodePropertyName = "code";
        internal const string StatusCodePropertyName = "statusCode";
        internal const string ReasonPropertyName = "message";

        /// <summary>Ably error code (see https://github.com/ably/ably-common/blob/master/protocol/errors.json) </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>The http status code corresponding to this error</summary>
        [JsonProperty("statusCode")]
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>Additional reason information, where available</summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        public bool IsUnAuthorizedError => StatusCode.HasValue &&
                                           StatusCode.Value == HttpStatusCode.Unauthorized;

        public bool IsTokenError => Code >= Defaults.TokenErrorCodesRangeStart &&
                                    Code <= Defaults.TokenErrorCodesRangeEnd;

        public ErrorInfo() { }

        public ErrorInfo(string reason)
        {
            Message = reason;
        }

        public ErrorInfo(string reason, int code)
        {
            Code = code;
            Message = reason;
        }

        public ErrorInfo(string reason, int code, HttpStatusCode? statusCode = null)
        {
            Code = code;
            StatusCode = statusCode;
            Message = reason;
        }

        public override string ToString()
        {
            if (StatusCode.HasValue == false)
            {
                return $"Reason: {Message}; Code: {Code}";
            }

            return $"Reason: {Message}; Code: {Code}; HttpStatusCode: {(int)StatusCode.Value} ({StatusCode})";
        }

        internal static ErrorInfo Parse(AblyResponse response)
        {
            string reason = string.Empty;
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
                    Debug.WriteLine(ex.Message);

                    // If there is no json or there is something wrong we don't want to throw from here. The
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

        public static bool IsRetryableStatusCode(HttpStatusCode statusCode)
        {
            return statusCode >= (HttpStatusCode)500 && statusCode <= (HttpStatusCode)504;
        }
    }
}