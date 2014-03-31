using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace Ably
{
    public class ErrorInfo
    {
        public int Code { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string Reason { get; set; }

        public ErrorInfo(string reason, int code)
        {
            Code = code;
            Reason = reason;
        }

        public ErrorInfo(string reason, int code, HttpStatusCode? statusCode = null)
        {
            Code = code;
            StatusCode = statusCode;
            Reason = reason;
        }

        public override string ToString()
        {
            if (StatusCode.HasValue == false)
            {
               return string.Format("Reason: {0}; Code: {1}", Reason, Code);
            }
            return string.Format("Reason: {0}; Code: {1}; HttpStatusCode: ({2}){3}", Reason, Code, (int)StatusCode.Value, StatusCode);
        }

        public static ErrorInfo Parse(AblyResponse response)
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
                        reason = (string)json["error"]["reason"];
                        errorCode = (int)json["error"]["code"];
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                    //If there is no json or there is something wrong we don't want to throw from here. The
                }
            }
            return new ErrorInfo(reason.IsEmpty() ? "Unknown error" : reason, errorCode, response.StatusCode);
        }

    }

    public class AblyException : Exception
    {
        public AblyException()
        {

        }

        public AblyException(string reason)
            : this(new ErrorInfo(reason, 500, null))
        {

        }

        public AblyException(Exception ex)
            : this(new ErrorInfo("Unexpected error :" + ex.Message, 50000, HttpStatusCode.InternalServerError))
            {
                
            }

        public AblyException(string reason, int code, HttpStatusCode? statusCode = null) : this(new ErrorInfo(reason, code, statusCode))
        {
            
        }

        public AblyException(ErrorInfo info)
            : base(info.ToString())
        {
            ErrorInfo = info;
        }


        public AblyException(ErrorInfo info, Exception innerException)
            : base(info.ToString(), innerException)
        {

        }

        protected AblyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public ErrorInfo ErrorInfo { get; set; }

        public static AblyException FromResponse(AblyResponse response)
        {
            return new AblyException(ErrorInfo.Parse(response));
        }
    }
}