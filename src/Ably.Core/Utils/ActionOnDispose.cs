using System;

namespace IO.Ably
{
    /// <summary>Utility class that implements IDisposable bu calling the provided action.</summary>
    internal class ActionOnDispose : IDisposable
    {
        readonly Action m_act;

        public ActionOnDispose( Action act )
        {
            if( null == act )
                throw new ArgumentNullException();
            m_act = act;
        }

        void IDisposable.Dispose()
        {
            m_act();
        }
    }
}