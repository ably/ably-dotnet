using System;
using System.Diagnostics;
using System.Text;

namespace Ably
{
    /// <summary>
    /// Default Ably logger. It logs Info and Error information as TraceEvents
    /// The Debug call writes a line in the debug console.
    /// </summary>
    public class Logger : ILogger
    {
        private static readonly ExtendedSource trace = new ExtendedSource("Ably", SourceLevels.Error);
        public static Logger Current = new Logger();

        private Logger()
        {

        }

        public virtual void Error(string message, Exception ex)
        {
            trace.TraceEvent(TraceEventType.Error, 0, String.Format("{0} {1}", message, GetExceptionDetails(ex)));
        }

        public virtual void Error(string message, params object[] args)
        {
            trace.TraceEvent(TraceEventType.Error, 0, String.Format(message, args));
        }

        public virtual void Info(string message, params object[] args)
        {
            trace.TraceEvent(TraceEventType.Information, 0, String.Format(message, args));
        }

        public void Verbose(string message, params object[] args)
        {
            trace.TraceEvent(TraceEventType.Verbose, 0, String.Format(message, args));
        }

        public virtual IDisposable ProfileOperation(string format, params object[] args)
        {
            return trace.ProfileOperation(format, args);
        }

        public virtual void Debug(string message)
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
