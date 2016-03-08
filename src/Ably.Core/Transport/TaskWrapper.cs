using System;
using System.Threading.Tasks;

namespace IO.Ably.Transport
{
    /// <summary>This trivial class wraps legacy callback-style API into a Task API.</summary>
    class TaskWrapper
    {
        readonly TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

        public static implicit operator Task( TaskWrapper wrapper ) { return wrapper.m_tcs.Task; }

        public static implicit operator Action<bool, ErrorInfo>( TaskWrapper wrapper ) { return wrapper.callback; }

        public void callback( bool res, ErrorInfo ei)
        {
            if( res )
                m_tcs.SetResult( true );
            if( null != ei )
                m_tcs.SetException( ei.AsException() );
            m_tcs.SetException( new Exception("") );
        }
    }
}