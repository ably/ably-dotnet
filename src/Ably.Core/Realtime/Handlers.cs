using System;
using System.Collections.Generic;

namespace IO.Ably.Realtime
{
    using TheSet = HashSet<WeakReference<IMessageHandler>>;

    /// <summary>This specialized collection keeps a set of weak references to IMessageHandler instances.</summary>
    internal class Handlers
    {
        readonly TheSet m_set = new TheSet();

        public IEnumerable<IMessageHandler> alive()
        {
            TheSet deadSet = null;
            foreach( var wr in m_set )
            {
                IMessageHandler res;
                if( wr.TryGetTarget( out res ) )
                {
                    yield return res;
                    continue;
                }
                // Dead
                if( null == deadSet )
                    deadSet = new TheSet();
                deadSet.Add( wr );
            }
            m_set.ExceptWith( deadSet );
        }

        /// <summary>Add handler to the collection.</summary>
        /// <param name="handler"></param>
        public void add( IMessageHandler handler )
        {
            if( null == handler )
                throw new ArgumentNullException();

            m_set.Add( new WeakReference<IMessageHandler>( handler ) );
        }

        /// <summary>Remove handler from the collection.</summary>
        /// <param name="handler"></param>
        /// <returns>True if removed, false if not found.</returns>
        public bool remove( IMessageHandler handler )
        {
            bool found = false;
            TheSet setToRemove = new TheSet();
            foreach( var wr in m_set )
            {
                IMessageHandler res;
                if( wr.TryGetTarget( out res ) )
                {
                    if( res != handler )
                        continue;
                    // Found the requested handler, and it's alive.
                    found = true;
                    setToRemove.Add( wr );
                }
                // Dead
                setToRemove.Add( wr );
            }
            m_set.ExceptWith( setToRemove );
            return found;
        }
    }
}