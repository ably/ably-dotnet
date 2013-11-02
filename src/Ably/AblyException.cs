using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace Ably
{
    public class AblyException : Exception
    {
        public AblyException()
        {

        }
        public AblyException(string message)
            : base(message)
        {

        }
        public AblyException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected AblyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public HttpStatusCode? HttpStatusCode { get; set; }
        public string ErrorCode { get; set; }
        private string _reason;
        public string Reason { get { return _reason ?? Message; } set { _reason = value; } }

        public static AblyException FromResponse(AblyResponse response)
        {
            string reason = "";
            string errorCode = "";
            try
            {
                var json = JObject.Parse(response.JsonResult);
                if (json["error"] != null)
                {
                    reason = (string)json["error"]["reason"];
                    errorCode = (string)json["error"]["code"];
                }
            }
            catch (Exception)
            {
                //If there is no json or there is something wrong we don't want to throw from here. The
            }

            string message = errorCode.IsNotEmpty() ? string.Format("{0}: {1}", errorCode, reason) : "Something went wrong. Response: " + response.JsonResult;
            var exception = new AblyException(message)
                {
                    HttpStatusCode = response.StatusCode,
                    Reason = reason,
                    ErrorCode = errorCode
                };
            return exception;
        }
    }
}