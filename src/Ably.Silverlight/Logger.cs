using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;

namespace Ably
{
    public class Logger : ILogger
    {
        public static Logger Current = new Logger();
        private IsolatedStorageFile _file;

        private Logger()
        {
            _file = IsolatedStorageFile.GetUserStoreForSite();
        }

        public void Debug(string message)
        {
            Log("Debug", message);
        }

        public void Error(string message, params object[] args)
        {
            Log("Error", message, args);
        }

        public void Error(string message, Exception ex)
        {
            Log("Error", String.Format("{0} {1}", message, GetExceptionDetails(ex)));
        }

        public void Info(string message, params object[] args)
        {
            Log("Info", message, args);
        }

        public void Verbose(string message, params object[] args)
        {
            Log("Verbose", message, args);
        }

        private void Log(string type, string message, params object[] args)
        {
            Log(type, string.Format(message, args));
        }

        private void Log(string type, string message)
        {
            string logMessage = string.Format("{0} {1}: {2}", DateTime.Now, type.ToUpper(), message);
            using (IsolatedStorageFileStream fs = new IsolatedStorageFileStream("log.txt", FileMode.Append, _file))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine(logMessage);
                }
            }
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
