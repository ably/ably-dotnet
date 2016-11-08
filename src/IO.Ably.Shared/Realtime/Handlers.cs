using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IO.Ably.Realtime
{
    internal class Handlers<T> where T : IMessage
    {
        private readonly List<MessageHandlerAction<T>> _handlers = new List<MessageHandlerAction<T>>();
        private readonly Dictionary<string, List<MessageHandlerAction<T>>> _specificHandlers = new Dictionary<string, List<MessageHandlerAction<T>>>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public IEnumerable<MessageHandlerAction<T>> GetHandlers(string eventName = null)
        {
            try
            {
                _lock.EnterReadLock();
                if (eventName.IsNotEmpty())
                {
                    List<MessageHandlerAction<T>> result;
                    if (_specificHandlers.TryGetValue(eventName.ToLower(), out result))
                    {
                        return new List<MessageHandlerAction<T>>(result);
                    }
                    return Enumerable.Empty<MessageHandlerAction<T>>();
                }

                return new List<MessageHandlerAction<T>>(_handlers);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>Add handler to the collection.</summary>
        /// <param name="handler"></param>
        public void Add(MessageHandlerAction<T> handler)
        {
            try
            {
                _lock.EnterWriteLock();
                _handlers.Add(handler);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Add(string eventName, MessageHandlerAction<T> handler)
        {
            try
            {
                _lock.EnterWriteLock();
                List<MessageHandlerAction<T>> result;
                var key = eventName.ToLower();
                if (_specificHandlers.TryGetValue(key, out result))
                {
                    if (result != null)
                    {
                        result.Add(handler);
                        return;
                    }
                }
                _specificHandlers[key] = new List<MessageHandlerAction<T>> { handler };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Remove handler from the collection.</summary>
        /// <param name="handler"></param>
        /// <returns>True if removed, false if not found.</returns>
        public bool Remove(MessageHandlerAction<T> handler)
        {
            try
            {
                _lock.EnterWriteLock();
                return _handlers.RemoveAll(x => x.Equals(handler)) > 0;
            }
            finally 
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Remove(string eventName, MessageHandlerAction<T> handler = null)
        {
            if (eventName.IsEmpty())
                return false;

            try
            {
                _lock.EnterWriteLock();
                var key = eventName.ToLower();
                if (_specificHandlers.ContainsKey(key))
                {
                    if (handler == null)
                    {
                        _specificHandlers.Remove(key);
                        return true;
                    }
                    else
                    {
                        return _specificHandlers[key].Remove(handler);
                    }
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveAll()
        {
            try
            {
                _lock.EnterWriteLock();
                _specificHandlers.Clear();
                _handlers.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}