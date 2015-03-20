using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
{
    public interface ILogger
    {
        void Error(string message, Exception ex);
        void Error(string message, params object[] args);
        void Info(string message, params object[] args);
        void Debug(string message);
    }
}
