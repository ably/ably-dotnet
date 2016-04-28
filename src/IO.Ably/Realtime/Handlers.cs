using System;
using System.Collections.Generic;

namespace IO.Ably.Realtime
{
    using TheSet = HashSet<WeakReference<IMessageHandler>>;

    /// <summary>This specialized collection keeps a set of weak references to IMessageHandler instances.</summary>
    /// <remarks>The collection is not thread safe.</remarks>
    internal class Handlers
    {
        private readonly TheSet _set = new TheSet();

        public IEnumerable<IMessageHandler> GetAliveHandlers()
        {
            TheSet deadSet = new TheSet();
            foreach (var reference in _set)
            {
                IMessageHandler res;
                if (reference.TryGetTarget(out res))
                {
                    yield return res;
                    continue;
                }
                // Dead
                deadSet.Add(reference);
            }
            _set.ExceptWith(deadSet);
        }

        /// <summary>Add handler to the collection.</summary>
        /// <param name="handler"></param>
        public void Add(IMessageHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler), "Null handlers are not supported");

            _set.Add(new WeakReference<IMessageHandler>(handler));
        }

        /// <summary>Remove handler from the collection.</summary>
        /// <param name="handler"></param>
        /// <returns>True if removed, false if not found.</returns>
        public bool Remove(IMessageHandler handler)
        {
            var removed = false;
            var setToRemove = new TheSet();
            foreach (var reference in _set)
            {
                IMessageHandler res;
                if (reference.TryGetTarget(out res))
                {
                    if (res != handler)
                        continue;
                    // Found the requested handler, and it's alive.
                    removed = true;
                    setToRemove.Add(reference);
                }
                // Dead
                setToRemove.Add(reference);
            }
            _set.ExceptWith(setToRemove);
            return removed;
        }
    }
}