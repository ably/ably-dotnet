using System;

namespace IO.Ably
{
    /// <summary>
    /// Add interface documentation...
    /// </summary>
    internal interface IInternalLogger : ILogger
    {
        /// <summary>
        /// Add method documentation...
        /// </summary>
        /// <param name="i">Add param documentation...</param>
        /// <returns>Add return type documentation...</returns>
        IDisposable SetTempDestination(ILoggerSink i);
    }
}
