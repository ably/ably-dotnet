using System;
using System.Diagnostics;
using System.Text;

namespace Ably
{
    /// <summary>Default Ably logger.</summary>
    /// <remarks>It logs messages as TraceEvents. In addition, the Debug call writes a line in the debug console.</remarks>
    /// </summary>
    public class Logger : ILogger
    {
        readonly SourceLevels levels;
        readonly ExtendedSource trace;

        static Logger s_current;

        /// <summary>An object you can write log messages to.</summary>
        public static ILogger Current { get { return s_current; } }

        const SourceLevels defaultLogLevels = SourceLevels.Error;

        static Logger()
        {
            s_current = new Logger( defaultLogLevels );
        }

        /// <summary> A bitwise combination of the enumeration values that specifies the source level at which to log.</summary>
        public static SourceLevels logLevel
        {
            get { return s_current.levels; }
            set
            {
                if( s_current.levels != value )
                    s_current = new Logger( value );
            }
        }

        private Logger( SourceLevels levels )
        {
            this.levels = levels;
            this.trace = new ExtendedSource( "Ably", this.levels );
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
            trace.TraceEvent( TraceEventType.Verbose, 0, message );
            if( 0 != ( this.levels & SourceLevels.Verbose ) )
                System.Diagnostics.Debug.WriteLine( message );
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
