using System;
using System.Threading.Tasks;

namespace IO.Ably.Transport
{
    /// <summary>This trivial class wraps legacy callback-style API into a Task API.</summary>
    class TaskWrapper
    {
        readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

        /// <summary>Operator that casts to <see cref="Task" />.</summary>
        public static implicit operator Task( TaskWrapper wrapper ) { return wrapper._completionSource.Task; }

        /// <summary>Operator that casts to the callback action type.</summary>
        public static implicit operator Action<bool, ErrorInfo>( TaskWrapper wrapper ) { return wrapper.Callback; }

        void Callback( bool res, ErrorInfo ei)
        {
            if( res )
                _completionSource.SetResult( true );
            if( null != ei )
                _completionSource.SetException( ei.AsException() );
            _completionSource.SetException( new Exception("") );
        }
    }
}