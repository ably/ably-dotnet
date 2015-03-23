using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ably
{
    internal class Logger : ILogger
    {
        private static readonly ExtendedSource trace = new ExtendedSource("Ably", SourceLevels.Error);
        public static Logger Current = new Logger();

        private Logger()
        {

        }

        public void Error(string message, Exception ex)
        {
            trace.TraceEvent(TraceEventType.Error, 0, String.Format("{0} {1}", message, GetExceptionDetails(ex)));
        }

        public void Error(string message, params object[] args)
        {
            trace.TraceEvent(TraceEventType.Error, 0, String.Format(message, args));
        }

        public void Info(string message, params object[] args)
        {
            trace.TraceEvent(TraceEventType.Information, 0, String.Format(message, args));
        }

        public void Verbose(string message, params object[] args)
        {
            trace.TraceEvent(TraceEventType.Verbose, 0, String.Format(message, args));
        }

        public IDisposable ProfileOperation(string format, params object[] args)
        {
            return trace.ProfileOperation(format, args);
        }

        public void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        private string GetExceptionDetails(Exception ex)
        {
            var message = new StringBuilder();
            var webException = ex as AblyException;
            if (webException != null)
            {
                message.AppendLine("Error code: " + webException.ErrorInfo.Code);
                message.AppendLine("Status code: " + webException.ErrorInfo.StatusCode);
                message.AppendLine("Reason: " + webException.ErrorInfo.Reason);
            }

            message.AppendLine(ex.Message);
            message.AppendLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                message.AppendLine("Inner exception:");
                message.AppendLine(GetExceptionDetails(ex.InnerException));
            }
            return message.ToString();
        }


    }
}
