using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ably
{
    /// <summary>
    /// Inherits from <see cref="TraceSource" /> and adds additional methods for profiling methods (i.e. tracing Start and Stop).
    /// </summary>
    /// <remarks>
    /// In use you can utilise the ProfileOperation methods on this trace source to simply output start and stop events for methods. These methods
    /// return an object that implements the <see cref="System.IDisposable">IDisposable</see> method, so are designed to be utilised within a
    /// 'using' clause, such as in the following example code.
    /// <code>
    /// public void SomeMethod()
    /// {
    ///     using ( MySource.ProfileOperation("SomeMethod"))
    ///     {
    ///         // Code here as necessary
    ///     }
    /// }
    /// </code>
    /// Here I have assumned that there is a trace source called MySource available to the code. When executed this code will emit
    /// start and stop events into the log. There are various overloads of the ProfileOperation() method which take different parameters.
    /// </remarks>
    internal class ExtendedSource : TraceSource
    {
        /// <summary>
        /// Construct the extended source
        /// </summary>
        /// <param name="sourceName">The name of the source</param>
        public ExtendedSource(string sourceName)
            : base(sourceName)
        {
            Action<bool> bo = tf => Console.Write(tf);
        }

        /// <summary>
        /// Construct the ExtendedSource
        /// </summary>
        /// <param name="sourceName">The name of the source</param>
        /// <param name="defaultLevel">The default source level</param>
        public ExtendedSource(string sourceName, SourceLevels defaultLevel)
            : base(sourceName, defaultLevel)
        {
        }

        /// <summary>
        /// Method used to Profile an operation. A 'Start' level trace is logged when this method is called
        /// and a 'Stop' when the return type is disposed.
        /// </summary>
        /// <param name="message">The message that will be logged with each Start and Stop trace.</param>
        /// <returns>An <see cref="IDisposable"/> type that traces a 'Stop' event when disposed.</returns>
        public IDisposable ProfileOperation(string message)
        {
            return new MethodProfiler(this, message);
        }

        /// <summary>
        /// Method used to Profile an operation. A 'Start' level trace is logged when this method is called
        /// and a 'Stop' when the return type is disposed.
        /// </summary>
        /// <param name="format">The format string that will form the skeleton of the logged message</param>
        /// <param name="args">The parameters passed to the string.Format call.</param>
        /// <returns>An <see cref="IDisposable"/> type that traces a 'Stop' event when disposed.</returns>
        public IDisposable ProfileOperation(string format, params object[] args)
        {
            return new MethodProfiler(this, format, args);
        }

        /// <summary>
        /// Method used to Profile an operation. A 'Start' level trace is logged when this method is called
        /// and a 'Stop' when the return type is disposed.
        /// </summary>
        /// <param name="data">The data that will be logged with each Start and Stop trace</param>
        /// <returns>An <see cref="IDisposable"/> type that traces a 'Stop' event when disposed.</returns>
        public IDisposable ProfileOperation(object data)
        {
            return new MethodProfiler(this, data);
        }

        /// <summary>
        /// Method used to Profile an operation. A 'Start' level trace is logged when this method is called
        /// and a 'Stop' when the return type is disposed.
        /// </summary>
        /// <param name="data">The data that will be logged with each Start and Stop trace</param>
        /// <returns>An <see cref="IDisposable"/> type that traces a 'Stop' event when disposed.</returns>
        public IDisposable ProfileOperation(params object[] data)
        {
            return new MethodProfiler(this, data);
        }

        /// <summary>
        /// Private implementation of <see cref="IDisposable"/> that logs the actual start and stop events.
        /// </summary>
        private class MethodProfiler : IDisposable
        {
            private delegate void Disposal();

            private readonly Disposal _disposal;
            private bool _disposed;

            internal MethodProfiler(TraceSource source, string message)
            {
                source.TraceEvent(TraceEventType.Start, 0, message);
                _disposal = delegate() { source.TraceEvent(TraceEventType.Stop, 0, message); };
            }

            internal MethodProfiler(TraceSource source, string format, params object[] args)
            {
                source.TraceEvent(TraceEventType.Start, 0, format, args);
                _disposal = delegate() { source.TraceEvent(TraceEventType.Stop, 0, format, args); };
            }

            internal MethodProfiler(TraceSource source, object data)
            {
                source.TraceData(TraceEventType.Start, 0, data);
                _disposal = delegate() { source.TraceData(TraceEventType.Stop, 0, data); };
            }

            internal MethodProfiler(TraceSource source, params object[] data)
            {
                source.TraceData(TraceEventType.Start, 0, data);
                _disposal = delegate() { source.TraceData(TraceEventType.Stop, 0, data); };
            }

            void IDisposable.Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _disposal();
                }
            }
        }
    }
}
