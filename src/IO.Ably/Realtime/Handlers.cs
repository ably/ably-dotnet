using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IO.Ably.Realtime
{
    internal class Handlers
    {
        private readonly List<IMessageHandler> _handlers = new List<IMessageHandler>();
        private readonly Dictionary<string, List<IMessageHandler>> _specificHandlers = new Dictionary<string, List<IMessageHandler>>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public IEnumerable<IMessageHandler> GetHandlers(string eventName = null)
        {
            try
            {
                _lock.EnterReadLock();
                if (eventName.IsNotEmpty())
                {
                    List<IMessageHandler> result;
                    if (_specificHandlers.TryGetValue(eventName.ToLower(), out result))
                    {
                        return new List<IMessageHandler>(result);
                    }
                    return Enumerable.Empty<IMessageHandler>();
                }

                return new List<IMessageHandler>(_handlers);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>Add handler to the collection.</summary>
        /// <param name="handler"></param>
        public void Add(IMessageHandler handler)
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

        public void Add(string eventName, IMessageHandler handler)
        {
            try
            {
                _lock.EnterWriteLock();
                List<IMessageHandler> result;
                if (_specificHandlers.TryGetValue(eventName, out result))
                {
                    if (result != null)
                    {
                        result.Add(handler);
                        return;
                    }
                }
                _specificHandlers[eventName] = new List<IMessageHandler>() { handler };
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Remove handler from the collection.</summary>
        /// <param name="handler"></param>
        /// <returns>True if removed, false if not found.</returns>
        public bool Remove(IMessageHandler handler)
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

        public bool Remove(string eventName, IMessageHandler handler = null)
        {
            if (eventName.IsEmpty())
                return false;

            try
            {
                _lock.EnterWriteLock();
                if (_specificHandlers.ContainsKey(eventName))
                {
                    if (handler == null)
                    {
                        _specificHandlers.Remove(eventName);
                        return true;
                    }
                    else
                    {
                        return _specificHandlers[eventName].Remove(handler);
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