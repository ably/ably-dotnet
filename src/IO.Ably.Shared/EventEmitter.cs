using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IO.Ably
{
    public interface IEventEmitter<TEvent, TArgs> where TEvent : struct where TArgs : EventArgs
    {
        void Off();
        void On(Action<TArgs> listener);
        void Once(Action<TArgs> listener);
        void Off(Action<TArgs> listener);
        void On(TEvent @event, Action<TArgs> action);
        void Once(TEvent @event, Action<TArgs> action);
        void Off(TEvent @event, Action<TArgs> action);
    }

    public abstract class EventEmitter<TEvent, TArgs> : IEventEmitter<TEvent, TArgs> where TEvent : struct
        where TArgs : EventArgs
    {
        readonly List<Emitter<TEvent, TArgs>> _emitters = new List<Emitter<TEvent, TArgs>>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private class Emitter<TEvent, TArgs> where TEvent : struct
        {
            public Action<TArgs> Action { get; }
            public bool Once { get; }
            public TEvent? Event { get; }

            public Emitter(Action<TArgs> action, TEvent? @event = null, bool once = false)
            {
                if (action == null)
                {
                    throw new ArgumentException("Cannot pass a null action to the EventEmitter");
                }

                Action = action;
                Event = @event;
                Once = once;
            }
        }

        public void Off()
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void On(Action<TArgs> listener)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_emitters.Any(x => x.Action == listener) == false)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _emitters.Add(new Emitter<TEvent, TArgs>(listener));
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void Once(Action<TArgs> listener)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_emitters.Any(x => x.Action == listener) == false)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _emitters.Add(new Emitter<TEvent, TArgs>(listener, once: true));
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void Off(Action<TArgs> listener)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.RemoveAll(x => x.Action == listener);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void On(TEvent @event, Action<TArgs> action)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.Add(new Emitter<TEvent, TArgs>(action, @event));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Once(TEvent @event, Action<TArgs> action)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.Add(new Emitter<TEvent, TArgs>(action, @event, true));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Off(TEvent @event, Action<TArgs> action)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.RemoveAll(x => x.Action == action && x.Event.HasValue && x.Event.Value.Equals(@event));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Emit(TEvent @event, TArgs data)
        {
            List<Emitter<TEvent, TArgs>> emitters;
            _lock.EnterUpgradeableReadLock();
            try
            {
                emitters = _emitters.Where(x => x.Event == null
                                            || x.Event.Value.Equals(@event)).ToList();

                if (emitters.Any(x => x.Once))
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        foreach (var emitter in emitters.Where(x => x.Once))
                        {
                            _emitters.Remove(emitter);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }

            foreach (var emitter in emitters)
            {
                try
                {
                    emitter.Action(data);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error executing on event: {@event}", ex);
                }
            }
        }

    }
}
